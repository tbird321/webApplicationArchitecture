#!/usr/bin/env node
/*
 * check-stylometry.js — measure article prose against the owner's measured voice,
 * then review it for the structural fingerprints of machine-written text.
 *
 * The agent templates carry numeric targets taken from theron_corpus.jsonl. Targets that
 * live only in a prompt get approximated; this makes them checkable. Run it on a draft
 * BEFORE publishing.
 *
 *   node scripts/check-stylometry.js path/to/article.html [more.html ...]
 *   node scripts/check-stylometry.js --s3 ldsapologetics.com Born-Again.html
 *
 * Two passes run on every file:
 *
 *   1. VOICE   — sentence-length distribution, question rate, emphasis rate. Measured on the
 *                whole article, the way the corpus targets were calibrated.
 *   2. REVIEW  — phrase tells, vocabulary tells, and structural tells (paragraph uniformity,
 *                triad stacking, antithesis stacking, opener repetition, boilerplate headings).
 *                Measured on the AUTHOR'S prose only: blockquotes are stripped first, because
 *                quoted scripture is not the author writing and skews every one of these.
 *
 * Exit code 1 if any voice target is missed or the review verdict is not HUMAN-CONSISTENT,
 * so it can gate a publish step. Pass --lenient to gate on voice targets only.
 */
const fs = require('fs');

// Measured from the 986 corpus pieces of 250+ words (521,495 words of connected prose).
const TARGET = {
  meanWords:   { lo: 15,   hi: 21,   label: 'mean sentence length', ideal: '18' },
  shortShare:  { lo: 0.15, hi: 0.30, label: 'sentences under 8 words', ideal: '~22%', pct: true },
  longShare:   { lo: 0.08, hi: 0.20, label: 'sentences over 30 words', ideal: '~14%', pct: true },
  questShare:  { lo: 0.05, hi: 0.16, label: 'questions', ideal: '~10%', pct: true },
  capsPerK:    { lo: 2,    hi: 14,   label: 'ALL-CAPS words per 1,000', ideal: '~7' },
  stdevWords:  { lo: 9,    hi: 99,   label: 'sentence-length stdev (alternation)', ideal: '>=9' },
};

// ---------------------------------------------------------------------------
// Phrase tells. Shapes I actually over-used, plus the ones every model reaches for.
// severity 'fail' gates on a single hit; 'warn' needs to accumulate.
// ---------------------------------------------------------------------------
const AI_TELLS = [
  // --- antithesis and flip constructions: the single loudest machine tell ---
  { re: /\bit'?s not (?:just|only) [^.;]{3,40}(?:—|--|,) it'?s\b/gi, name: '"it\'s not just X, it\'s Y"', sev: 'fail' },
  { re: /\bthat is not [a-z ]{2,20}\. that is\b/gi,     name: '"That is not X. That is Y." flip', sev: 'fail' },
  { re: /\bthis isn'?t (?:about |just |merely )?[^.;]{2,40}[.;] (?:it'?s|this is|that'?s)\b/gi,
                                                        name: '"This isn\'t X. It\'s Y." flip', sev: 'fail' },
  { re: /\bnot merely\b/gi,                             name: '"not merely"', sev: 'fail' },
  { re: /\bnot only [^.;]{3,50} but also\b/gi,          name: '"not only X but also Y"', sev: 'warn' },
  { re: /\bmore than (?:just )?(?:a|an|the) [^.;]{3,40}[,;—-]/gi, name: '"more than just a…"', sev: 'warn' },

  // --- discourse scaffolding no human needs ---
  { re: /\bstated fairly\b/gi,                          name: '"Stated Fairly" heading formula', sev: 'fail' },
  { re: /\bthis is what\b/gi,                           name: '"This is what…" anaphora', sev: 'fail' },
  { re: /\bgrant (?:the|that|what)\b/gi,                name: '"Grant the…" concessive opener', sev: 'fail' },
  { re: /\blet'?s be clear\b/gi,                        name: '"let\'s be clear"', sev: 'fail' },
  { re: /\bit'?s worth noting\b/gi,                     name: '"it\'s worth noting"', sev: 'fail' },
  { re: /\bit'?s important to (?:note|remember|understand|recognize|recognise)\b/gi,
                                                        name: '"it\'s important to note"', sev: 'fail' },
  { re: /\bhere'?s the thing\b/gi,                      name: '"here\'s the thing"', sev: 'fail' },
  { re: /\bmake no mistake\b/gi,                        name: '"make no mistake"', sev: 'warn' },
  { re: /\bthe (?:simple |plain )?(?:truth|reality) is(?: that)?\b/gi, name: '"the truth is that…"', sev: 'warn' },
  { re: /\bat (?:its|the) (?:core|heart)\b/gi,          name: '"at its core"', sev: 'warn' },
  { re: /\bthis is where [^.;]{3,40} comes in\b/gi,     name: '"this is where X comes in"', sev: 'fail' },
  { re: /\bwhen it comes to\b/gi,                       name: '"when it comes to"', sev: 'warn' },
  { re: /\bin (?:today'?s|the modern) world\b|\bin conclusion\b|\bat the end of the day\b/gi,
                                                        name: 'essay filler', sev: 'fail' },
  { re: /\b(?:moreover|furthermore|additionally)\b/gi,  name: 'connective scaffolding (moreover/furthermore)', sev: 'warn' },
  { re: /\b(?:ultimately|essentially|fundamentally),/gi, name: 'sentence-initial "Ultimately,/Essentially,"', sev: 'warn' },

  // --- inflated vocabulary ---
  { re: /\bdelve\b|\btapestry\b|\bnuanced\b|\bmultifaceted\b|\bunderscore[sd]?\b/gi,
                                                        name: 'LLM vocabulary (delve/tapestry/nuanced)', sev: 'fail' },
  { re: /\b(?:a )?testament to\b|\bstands? as (?:a|the)\b|\bserves? as (?:a|the)\b/gi,
                                                        name: '"testament to" / "serves as a"', sev: 'warn' },
  { re: /\bplays? a (?:crucial|key|vital|significant|pivotal) role\b/gi, name: '"plays a crucial role"', sev: 'fail' },
  { re: /\b(?:realm|landscape) of\b/gi,                 name: '"realm of" / "landscape of"', sev: 'warn' },
  { re: /\b(?:seamless|robust|holistic|myriad|plethora|intricate|ever-evolving|cutting-edge|game-?changer)\b/gi,
                                                        name: 'brochure vocabulary', sev: 'warn' },
  { re: /\b(?:leverage|harness|unlock|elevate|foster|showcase|boasts?|resonates?)\b/gi,
                                                        name: 'marketing verbs', sev: 'warn' },
  { re: /\b(?:crucial|pivotal|profound|comprehensive|meticulous)\b/gi, name: 'intensifier vocabulary', sev: 'watch' },

  // --- hedging stacks ---
  { re: /\b(?:may|might|could) (?:potentially|possibly|perhaps|arguably)\b/gi, name: 'stacked hedging', sev: 'warn' },
  { re: /\bsome (?:would |might |may )?(?:argue|say|contend) that\b/gi, name: '"some would argue that"', sev: 'warn' },

  // --- tells found by reading finished drafts cold, not by theory. Every one of these
  //     survived the numeric checks above and still gave the page away. ---
  { re: /\b(?:in|at) (?:its|his|their|the) (?:strongest|best|most charitable) (?:form|version|light)\b/gi,
                                                        name: 'steelman preamble ("in its strongest form")', sev: 'fail' },
  { re: /\bgive (?:them|him|her|it) (?:full )?credit\b|\bto be (?:entirely |perfectly |scrupulously )?fair\b|\bsteel-?man\b/gi,
                                                        name: 'concede-then-pivot formula', sev: 'fail' },
  { re: /\bthe (?:weak|weakest|straw) version\b|\bwithout caricature\b|\bno straw ?man\b/gi,
                                                        name: '"not the weak version" disclaimer', sev: 'fail' },
  { re: /\b(?:two|three|four|five) (?:things|claims|reasons|problems|points|questions|answers|objections|facts)\b/gi,
                                                        name: 'enumerated preview ("three things…")', sev: 'warn' },
  { re: /\bno [^,.;]{2,28}, no [^,.;]{2,28}, (?:and )?no\b/gi, name: 'anaphoric negation triad', sev: 'warn' },
  { re: /\bhere is (?:the|what|why|where)\b|\bhere lies\b/gi, name: '"Here is the…" pointer', sev: 'warn' },
  { re: /\band that changes everything\b|\breorganis?[sz]es everything\b|\beverything (?:downstream|else) (?:of it )?follows\b/gi,
                                                        name: '"changes everything" escalation', sev: 'fail' },
];

// Sentence-opening imperatives aimed at the reader. A few are voice; a steady beat of them
// is a model stage-managing the reader through an argument.
const READER_IMPERATIVES =
  /^(?:notice|read|ask|consider|look|watch|note|picture|imagine|observe|remember|recall|compare|contrast|start with|take|put|open)\b/i;

// Headings a human writing for these sites does not use.
const BANNED_HEADINGS = [
  'conclusion', 'in conclusion', 'introduction', 'overview', 'key takeaways', 'takeaways',
  'final thoughts', 'summary', 'in summary', 'why it matters', 'the bottom line',
  'frequently asked questions', 'faq', 'tl;dr', 'closing thoughts', 'wrapping up',
];

// Openers that carry no information about repetition when they lead a sentence.
const STOP_OPENERS = new Set([
  'the', 'a', 'an', 'and', 'but', 'so', 'it', 'that', 'this', 'they', 'he', 'she', 'we',
  'you', 'i', 'there', 'then', 'now', 'if', 'in', 'on', 'at', 'to', 'for', 'what', 'who',
  'when', 'where', 'why', 'how', 'his', 'her', 'their', 'its', 'no', 'not', 'one', 'two',
]);

// ---------------------------------------------------------------------------
// Structural targets. These are shape checks, not word checks — a model can avoid
// every banned phrase above and still lay a page out like a template.
// ---------------------------------------------------------------------------
const STRUCTURE = {
  paraCV:        { lo: 0.30, hi: 9,  label: 'paragraph-length variation (CV)', ideal: '>=0.30', sev: 'fail',
                   why: 'uniform paragraph lengths are the clearest structural signature of generated text' },
  triadPerK:     { lo: 0,    hi: 9,  label: 'three-item lists per 1,000 words', ideal: '<=9', sev: 'warn',
                   why: 'rule-of-three stacking ("X, Y, and Z") is the most over-fitted rhythm in model prose' },
  antithesisPerK:{ lo: 0,    hi: 7,  label: 'antithesis constructions per 1,000', ideal: '<=7', sev: 'warn',
                   why: 'contrast flips read as profound once and as machinery by the fourth time' },
  emDashPerK:    { lo: 0,    hi: 28, label: 'em dashes per 1,000 words', ideal: '<=28', sev: 'warn',
                   why: 'the em dash is the tell most readers now consciously look for' },
  openerShare:   { lo: 0,    hi: 0.10, label: 'most-repeated sentence opener', ideal: '<=10%', pct: true, sev: 'warn',
                   why: 'anaphora across a whole page reads as templated rather than emphatic' },
  paraOpenerRun: { lo: 0,    hi: 2,  label: 'consecutive paragraphs sharing an opener', ideal: '<=2', sev: 'warn',
                   why: 'three paragraphs opening the same way is a generated cadence' },
  listCV:        { lo: 0.18, hi: 9,  label: 'bullet-length variation (CV)', ideal: '>=0.18', sev: 'warn',
                   why: 'bullets trimmed to matching lengths are written to a template, not to a point' },

  // Added after reading finished drafts cold. All five passed everything above.
  punchCloserShare: { lo: 0, hi: 0.55, label: 'sections ending on a one-line punchline', ideal: '<=55%', pct: true, sev: 'fail',
                   why: 'a mic-drop at every section break is the loudest rhythm tell in generated argument' },
  soloParaShare: { lo: 0, hi: 0.38, label: 'single-sentence paragraphs', ideal: '<=38%', pct: true, sev: 'warn',
                   why: 'staccato one-line paragraphs used as punctuation rather than as emphasis' },
  sectionCV:     { lo: 0.20, hi: 9,  label: 'section-length variation (CV)', ideal: '>=0.20', sev: 'warn',
                   why: 'sections of near-identical length were filled to a template, not written to an argument' },
  quoteEvenness: { lo: 0, hi: 0.85, label: 'sections carrying exactly one quote', ideal: '<=85%', pct: true, sev: 'warn',
                   why: 'one blockquote per section, always in the same slot, is layout by formula' },
  imperativePerK:{ lo: 0, hi: 6,  label: 'reader-imperatives per 1,000 words', ideal: '<=6', sev: 'warn',
                   why: 'Notice/Read/Ask/Consider as sentence openers: the model stage-managing the reader' },
  demonstrativeShare: { lo: 0, hi: 0.14, label: 'sentences opening "That is / This is / It is"', ideal: '<=14%', pct: true, sev: 'warn',
                   why: 'demonstrative openers are how generated prose glues one assertion to the next' },
};

function stripTags(s) {
  const ents = { '&mdash;': '—', '&ndash;': '–', '&ldquo;': '"', '&rdquo;': '"',
                 '&lsquo;': "'", '&rsquo;': "'", '&amp;': '&', '&hellip;': '…',
                 '&nbsp;': ' ', '&lt;': '<', '&gt;': '>', '&quot;': '"' };
  let out = s.replace(/<(script|style)[\s\S]*?<\/\1>/gi, ' ').replace(/<[^>]+>/g, ' ');
  for (const [k, v] of Object.entries(ents)) out = out.split(k).join(v);
  return out.replace(/\s+/g, ' ').trim();
}

// Everything, including quoted scripture. This is what the corpus targets were calibrated on.
function textOf(html) { return stripTags(html); }

// The author's own prose: quotations and their attribution labels removed, so a page
// carrying eight KJV blockquotes is not scored on Jacobean sentence structure.
function proseOf(html) {
  return stripTags(html.replace(/<blockquote[\s\S]*?<\/blockquote>/gi, ' '));
}

function sentences(text) {
  // Protect the abbreviations that appear constantly in this content so they do not
  // manufacture sentence breaks: scripture refs (3 Ne. 2:15), D&C, initials.
  const guarded = text
    .replace(/\b(Ne|Cor|Thes|Tim|Pet|Jn|Matt|Mos|Alma|Hel|Morm|Moro|Ether|Jacob|Enos|Abr|vs|v|cf|ch|St|Dr|Mr|Mrs|approx|c)\.\s/gi,
             (m) => m.replace('.', ''));
  return guarded
    .split(/(?<=[.!?…])["')\]]*\s+/)
    .map(s => s.trim())
    .filter(s => /[A-Za-z]/.test(s) && s.split(/\s+/).length > 1);
}

const wc = (s) => s.split(/\s+/).filter(Boolean).length;
const cv = (arr) => {
  if (arr.length < 2) return 0;
  const m = arr.reduce((a, b) => a + b, 0) / arr.length;
  if (!m) return 0;
  return Math.sqrt(arr.reduce((a, l) => a + (l - m) ** 2, 0) / arr.length) / m;
};

function measure(html) {
  const text = textOf(html);
  const sents = sentences(text);
  const lens = sents.map(wc);
  const words = lens.reduce((a, b) => a + b, 0);
  const n = sents.length || 1;
  const mean = words / n;
  const stdev = Math.sqrt(lens.reduce((a, l) => a + (l - mean) ** 2, 0) / n);
  const caps = (text.match(/\b[A-Z]{3,}\b/g) || []).filter(w => !/^(HTML|LDS|BOM|NT|OT|SEC|UMC|NHM|KJV|JST)$/.test(w));

  return {
    sentences: n,
    words,
    meanWords: mean,
    stdevWords: stdev,
    shortShare: lens.filter(l => l < 8).length / n,
    longShare: lens.filter(l => l > 30).length / n,
    questShare: sents.filter(s => s.trim().endsWith('?')).length / n,
    capsPerK: (caps.length / (words || 1)) * 1000,
  };
}

function review(html) {
  const prose = proseOf(html);
  const sents = sentences(prose);
  const words = sents.reduce((a, s) => a + wc(s), 0) || 1;
  const perK = (n) => (n / words) * 1000;

  // Paragraph and bullet shapes, author's prose only. Quoted scripture lives inside
  // blockquotes and is not the author writing, so it is removed before any shape is measured.
  const authorHtml = html.replace(/<blockquote[\s\S]*?<\/blockquote>/gi, ' ');
  const paras = [...authorHtml.matchAll(/<p\b[^>]*>([\s\S]*?)<\/p>/gi)]
    .map(m => stripTags(m[1]))
    .filter(t => wc(t) >= 4 && !/^[A-Z0-9 :&;.'’–—-]+$/.test(t)); // drop label paragraphs
  const bullets = [...html.matchAll(/<li\b[^>]*>([\s\S]*?)<\/li>/gi)].map(m => wc(stripTags(m[1])));
  const headings = [...html.matchAll(/<h[1-6]\b[^>]*>([\s\S]*?)<\/h[1-6]>/gi)].map(m => stripTags(m[1]));

  // Section shapes. Split on H2, ignore the front matter and any section that is pure apparatus.
  const chunks = html.split(/<h2\b/i).slice(1);
  const sections = chunks.map(c => {
    const body = c.replace(/<blockquote[\s\S]*?<\/blockquote>/gi, ' ');
    const ps = [...body.matchAll(/<p\b[^>]*>([\s\S]*?)<\/p>/gi)]
      .map(m => stripTags(m[1]))
      .filter(t => wc(t) >= 4 && !/^[A-Z0-9 :&;.'’–—-]+$/.test(t));
    return { paras: ps, words: ps.reduce((a, p) => a + wc(p), 0),
             quotes: (c.match(/<blockquote/gi) || []).length };
  }).filter(s => s.paras.length > 0);

  // A "punch closer" is a section signing off with a single short sentence on its own line.
  const punchClosers = sections.filter(s => {
    const last = s.paras[s.paras.length - 1];
    return sentences(last).length === 1 && wc(last) <= 16;
  }).length;
  const soloParas = paras.filter(p => sentences(p).length === 1).length;

  // Sentence openers across the page, and runs of paragraphs opening the same way.
  const openers = sents.map(s => (s.match(/[A-Za-z'’]+/) || [''])[0].toLowerCase());
  const counts = {};
  for (const o of openers) if (o && !STOP_OPENERS.has(o)) counts[o] = (counts[o] || 0) + 1;
  const topOpener = Object.entries(counts).sort((a, b) => b[1] - a[1])[0] || ['—', 0];

  const paraOpeners = paras.map(p => (p.match(/[A-Za-z'’]+/) || [''])[0].toLowerCase());
  let run = 1, maxRun = 1, runWord = paraOpeners[0] || '—';
  for (let i = 1; i < paraOpeners.length; i++) {
    if (paraOpeners[i] && paraOpeners[i] === paraOpeners[i - 1]) {
      run++;
      if (run > maxRun) { maxRun = run; runWord = paraOpeners[i]; }
    } else run = 1;
  }

  const triads = prose.match(
    /\b[A-Za-z][\w'’-]*(?: [\w'’-]+){0,2}, [A-Za-z][\w'’-]*(?: [\w'’-]+){0,2},? and [A-Za-z][\w'’-]*(?: [\w'’-]+){0,2}\b/g) || [];
  const antith = [
    /\bnot (?:just |merely |only )?[^.;,]{2,40}, but\b/gi,
    /\bit'?s not [^.;]{2,50}[,—-] it'?s\b/gi,
    /\brather than\b/gi,
    /\binstead of\b/gi,
  ].reduce((a, re) => a + (prose.match(re) || []).length, 0);

  const imperatives = sents.filter(s => READER_IMPERATIVES.test(s.trim())).length;
  const demonstratives = sents.filter(s => /^(?:that|this|it)\s+(?:is|was|are|were)\b/i.test(s.trim())).length;

  const structure = {
    paraCV:         cv(paras.map(wc)),
    triadPerK:      perK(triads.length),
    antithesisPerK: perK(antith),
    emDashPerK:     perK((prose.match(/—/g) || []).length),
    openerShare:    topOpener[1] / (sents.length || 1),
    paraOpenerRun:  maxRun,
    listCV:         bullets.length >= 3 ? cv(bullets) : 1,
    punchCloserShare:   sections.length ? punchClosers / sections.length : 0,
    soloParaShare:      paras.length ? soloParas / paras.length : 0,
    sectionCV:          sections.length >= 3 ? cv(sections.map(s => s.words)) : 1,
    quoteEvenness:      sections.length ? sections.filter(s => s.quotes === 1).length / sections.length : 0,
    imperativePerK:     perK(imperatives),
    demonstrativeShare: demonstratives / (sents.length || 1),
  };

  const tells = AI_TELLS
    .map(t => ({ name: t.name, sev: t.sev, count: (prose.match(t.re) || []).length,
                 sample: (prose.match(t.re) || [])[0] }))
    .filter(t => t.count > 0);

  const badHeadings = headings.filter(h => BANNED_HEADINGS.includes(h.trim().toLowerCase().replace(/[.:—-]+$/, '')));

  return { structure, tells, badHeadings, proseWords: words,
           notes: { topOpener, runWord, paragraphs: paras.length, bullets: bullets.length } };
}

function report(label, html) {
  const m = measure(html);
  const r = review(html);
  let voiceFails = 0, reviewFails = 0, reviewWarns = 0;

  console.log(`\n=== ${label}`);
  console.log(`    ${m.sentences} sentences, ${m.words} words (${r.proseWords} of them the author's own prose)`);

  console.log(`\n  VOICE`);
  for (const [key, t] of Object.entries(TARGET)) {
    const raw = m[key];
    const val = t.pct ? raw * 100 : raw;
    const lo = t.pct ? t.lo * 100 : t.lo;
    const hi = t.pct ? t.hi * 100 : t.hi;
    const ok = val >= lo && val <= hi;
    if (!ok) voiceFails++;
    console.log(`    ${ok ? 'ok  ' : 'MISS'} ${t.label.padEnd(38)} ${val.toFixed(1)}${t.pct ? '%' : ''}  (target ${t.ideal})`);
  }

  console.log(`\n  REVIEW — does this read as machine-written?`);
  for (const [key, t] of Object.entries(STRUCTURE)) {
    const raw = r.structure[key];
    const val = t.pct ? raw * 100 : raw;
    const lo = t.pct ? t.lo * 100 : t.lo;
    const hi = t.pct ? t.hi * 100 : t.hi;
    const ok = val >= lo && val <= hi;
    if (!ok) { if (t.sev === 'fail') reviewFails++; else reviewWarns++; }
    const shown = val >= 10 ? val.toFixed(1) : val.toFixed(2);
    console.log(`    ${ok ? 'ok  ' : (t.sev === 'fail' ? 'MISS' : 'warn')} ${t.label.padEnd(38)} ${shown}${t.pct ? '%' : ''}  (target ${t.ideal})`);
    if (!ok) console.log(`         ${t.why}`);
  }
  if (r.structure.openerShare > STRUCTURE.openerShare.hi)
    console.log(`         repeated opener: "${r.notes.topOpener[0]}" x${r.notes.topOpener[1]}`);
  if (r.structure.paraOpenerRun > STRUCTURE.paraOpenerRun.hi)
    console.log(`         ${r.structure.paraOpenerRun} paragraphs in a row open with "${r.notes.runWord}"`);

  if (r.badHeadings.length) {
    reviewFails++;
    console.log(`    MISS boilerplate headings: ${r.badHeadings.join(', ')}`);
  }

  if (r.tells.length) {
    console.log(`    ${r.tells.some(t => t.sev === 'fail') ? 'MISS' : 'warn'} phrase and vocabulary tells:`);
    for (const t of r.tells) {
      const tag = t.sev === 'fail' ? 'FAIL' : t.sev === 'warn' ? 'warn' : 'note';
      if (t.sev === 'fail') reviewFails += t.count;
      else if (t.sev === 'warn') reviewWarns += t.count;
      else if (t.count >= 3) reviewWarns++;
      console.log(`         ${tag} ${String(t.count).padStart(2)}x  ${t.name}${t.sample ? `  — "${t.sample.trim().slice(0, 58)}"` : ''}`);
    }
  } else {
    console.log(`    ok   no phrase or vocabulary tells`);
  }

  const score = reviewFails * 2 + reviewWarns;
  const verdict = reviewFails === 0 && score <= 3 ? 'HUMAN-CONSISTENT'
                : reviewFails === 0 || score <= 8 ? 'SUSPECT'
                : 'MACHINE-LIKE';
  console.log(`\n  VERDICT  ${verdict}  (${reviewFails} hard, ${reviewWarns} soft, score ${score})`);
  if (verdict !== 'HUMAN-CONSISTENT')
    console.log(`           rewrite the flagged lines in your own cadence, then re-run.`);

  return { voiceFails, reviewOk: verdict === 'HUMAN-CONSISTENT' };
}

const args = process.argv.slice(2).filter(a => a !== '--lenient');
const lenient = process.argv.includes('--lenient');
let failures = 0;
if (args[0] === '--s3') {
  const { execSync } = require('child_process');
  const [, domain, ...files] = args;
  for (const f of files) {
    const html = execSync(
      `aws s3 cp s3://www-websitecontent/public/websites/${domain}/articles/${f} - --profile tbirdcontractinggmailcom --region us-west-2`,
      { encoding: 'utf8', maxBuffer: 1 << 26 });
    const res = report(`${domain}/${f}`, html);
    failures += res.voiceFails + (lenient || res.reviewOk ? 0 : 1);
  }
} else {
  for (const f of args) {
    const res = report(f, fs.readFileSync(f, 'utf8'));
    failures += res.voiceFails + (lenient || res.reviewOk ? 0 : 1);
  }
}
console.log(failures ? `\n${failures} check(s) failed.` : '\nAll checks passed.');
process.exit(failures ? 1 : 0);

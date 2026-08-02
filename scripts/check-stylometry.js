#!/usr/bin/env node
/*
 * check-stylometry.js — measure article prose against the owner's measured voice.
 *
 * The agent templates carry numeric targets taken from theron_corpus.jsonl. Targets that
 * live only in a prompt get approximated; this makes them checkable. Run it on a draft
 * BEFORE publishing.
 *
 *   node scripts/check-stylometry.js path/to/article.html [more.html ...]
 *   node scripts/check-stylometry.js --s3 ldsapologetics.com Born-Again.html
 *
 * Exit code 1 if any file misses a target, so it can gate a publish step.
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

// Phrases that read as machine-written. Most are shapes I actually over-used; the point is
// that ANY of them repeating across a page is the tell, not that the phrase is forbidden once.
const AI_TELLS = [
  { re: /\bit'?s not (?:just|only) [^.;]{3,40}(?:—|--|,) it'?s\b/gi, name: '"it\'s not just X, it\'s Y"' },
  { re: /\bnot merely\b/gi,                     name: '"not merely"' },
  { re: /\bthat is not [a-z ]{2,20}\. that is\b/gi, name: '"That is not X. That is Y." flip' },
  { re: /\bstated fairly\b/gi,                  name: '"Stated Fairly" heading formula' },
  { re: /\bthis is what\b/gi,                   name: '"This is what…" anaphora' },
  { re: /\bgrant (?:the|that|what)\b/gi,        name: '"Grant the…" concessive opener' },
  { re: /\blet'?s be clear\b/gi,                name: '"let\'s be clear"' },
  { re: /\bit'?s worth noting\b/gi,             name: '"it\'s worth noting"' },
  { re: /\bdelve\b|\btapestry\b|\bnuanced\b|\bmultifaceted\b|\bunderscore[sd]?\b/gi, name: 'LLM vocabulary' },
  { re: /\bin today'?s world\b|\bin conclusion\b/gi, name: 'essay filler' },
];

function textOf(html) {
  let s = html
    .replace(/<(script|style)[\s\S]*?<\/\1>/gi, ' ')
    .replace(/<[^>]+>/g, ' ');
  const ents = { '&mdash;': '—', '&ndash;': '–', '&ldquo;': '"', '&rdquo;': '"',
                 '&lsquo;': "'", '&rsquo;': "'", '&amp;': '&', '&hellip;': '…',
                 '&nbsp;': ' ', '&lt;': '<', '&gt;': '>', '&quot;': '"' };
  for (const [k, v] of Object.entries(ents)) s = s.split(k).join(v);
  return s.replace(/\s+/g, ' ').trim();
}

function sentences(text) {
  // Protect the abbreviations that appear constantly in this content so they do not
  // manufacture sentence breaks: scripture refs (3 Ne. 2:15), D&C, initials.
  const guarded = text
    .replace(/\b(Ne|Cor|Thes|Tim|Pet|Jn|Matt|Mos|Alma|Hel|Morm|Moro|Ether|Jacob|Enos|Abr|vs|v|cf|ch|St|Dr|Mr|Mrs|approx|c)\.\s/gi,
             (m) => m.replace('.', ''));
  return guarded
    .split(/(?<=[.!?…])["')\]]*\s+/)
    .map(s => s.trim())
    .filter(s => /[A-Za-z]/.test(s) && s.split(/\s+/).length > 1);
}

function measure(html) {
  const text = textOf(html);
  const sents = sentences(text);
  const lens = sents.map(s => s.split(/\s+/).filter(Boolean).length);
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
    tells: AI_TELLS.map(t => ({ name: t.name, count: (text.match(t.re) || []).length }))
                   .filter(t => t.count > 0),
  };
}

function report(label, m) {
  let failed = 0;
  console.log(`\n=== ${label}`);
  console.log(`    ${m.sentences} sentences, ${m.words} words`);
  for (const [key, t] of Object.entries(TARGET)) {
    const raw = m[key];
    const val = t.pct ? raw * 100 : raw;
    const lo = t.pct ? t.lo * 100 : t.lo;
    const hi = t.pct ? t.hi * 100 : t.hi;
    const ok = val >= lo && val <= hi;
    if (!ok) failed++;
    console.log(`    ${ok ? 'ok  ' : 'MISS'} ${t.label.padEnd(38)} ${val.toFixed(1)}${t.pct ? '%' : ''}  (target ${t.ideal})`);
  }
  if (m.tells.length) {
    failed++;
    console.log(`    MISS AI tells present:`);
    for (const t of m.tells) console.log(`         ${t.count}x  ${t.name}`);
  }
  return failed;
}

const args = process.argv.slice(2);
let failures = 0;
if (args[0] === '--s3') {
  const { execSync } = require('child_process');
  const [, domain, ...files] = args;
  for (const f of files) {
    const html = execSync(
      `aws s3 cp s3://www-websitecontent/public/websites/${domain}/articles/${f} - --profile tbirdcontractinggmailcom --region us-west-2`,
      { encoding: 'utf8', maxBuffer: 1 << 26 });
    failures += report(`${domain}/${f}`, measure(html));
  }
} else {
  for (const f of args) failures += report(f, measure(fs.readFileSync(f, 'utf8')));
}
console.log(failures ? `\n${failures} target(s) missed.` : '\nAll targets met.');
process.exit(failures ? 1 : 0);

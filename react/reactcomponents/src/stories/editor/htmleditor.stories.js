// HtmlEditor.stories.js
import React, { useState } from "react";
import HtmlEditor from "../../components/editor/HtmlEditor";

import { action } from "@storybook/addon-actions";

const images =  [
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/1059759.jpg", alt: "1059759" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/1stPeter.jpg", alt: "1stPeter" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/adam-eve-altar-39689-print.jpg", alt: "adam-eve-altar-39689-print" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/akiane-kramarikJesus.jpg", alt: "akiane-kramarikJesus" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/Alma36 Chiasmus.PNG", alt: "Alma36 Chiasmus" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/AmericaDefenseBudget.gif", alt: "AmericaDefenseBudget" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/AmericaDefenseBudget.jpg", alt: "AmericaDefenseBudget" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/ArmorOfGodImage.jpg", alt: "ArmorOfGodImage" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/BomPlates.jpg", alt: "BomPlates" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/bomplates_eofmp196.gif", alt: "bomplates_eofmp196" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/cake.jpg", alt: "cake" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/CandleSticks.jpg", alt: "CandleSticks" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/ChildDeathsPerYear.JPG", alt: "ChildDeathsPerYear" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/Chinese Compass.jpg", alt: "Chinese Compass" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/ChurchDenominations.jpg", alt: "ChurchDenominations" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/compass1.jpg", alt: "compass1" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/EuphratesRiverMap.gif", alt: "EuphratesRiverMap" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/ExplainationOfPlates.jpg", alt: "ExplainationOfPlates" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/ezra-taft-benson_1116887_tmb.jpg", alt: "ezra-taft-benson" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/GoldOreDeposits.png", alt: "GoldOreDeposits" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/HangOntoTheRod.jpg", alt: "HangOntoTheRod" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/headerLogo.png", alt: "headerLogo" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/JacobAndEsau.PNG", alt: "JacobAndEsau" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/JewelsOfBible.jpg", alt: "JewelsOfBible" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/john-baptizes-christ-39544-tablet.jpg", alt: "john-baptizes-christ-39544-tablet" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/JordanRiver.jpg", alt: "JordanRiver" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/lehisdream_1440x9601.jpg", alt: "lehisdream_1440x9601" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/LiaHoNa_by_xIchixCoolxGirlx.jpg", alt: "LiaHoNa_by_xIchixCoolxGirlx" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/LionAndLamb.jpg", alt: "LionAndLamb" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/LogoText.PNG", alt: "LogoText" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/Magnify.JPG", alt: "Magnify" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/Map-Canaan-Twelve-Tribes.gif", alt: "Map-Canaan-Twelve-Tribes" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/MyPicture.png", alt: "MyPicture" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/NationsAttackingIsrael.jpg", alt: "NationsAttackingIsrael" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/Noahsworld_map.jpg", alt: "Noahsworld_map" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/ocean-1.jpg", alt: "ocean-1" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/old-and-new-testaments.jpg", alt: "old-and-new-testaments" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/olive-tree.jpg", alt: "olive-tree" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/Olives.JPG", alt: "Olives" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/Optical-Illusion2.jpg", alt: "Optical-Illusion2" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/OpticalI-Illusion1.jpg", alt: "OpticalI-Illusion1" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/OpticalIllusion2.gif", alt: "OpticalIllusion2" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/Ordained.jpg", alt: "Ordained" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/papyrus-sealed-with-seven-seals_1246685_inl.jpg", alt: "papyrus-sealed-with-seven-seals" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/PlanOfSalvation.PNG", alt: "PlanOfSalvation" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/plan_temple.gif", alt: "plan_temple" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/PriestGarments.jpg", alt: "PriestGarments" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/scriptureIcon.jpg", alt: "scriptureIcon" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/scriptures.jpg", alt: "scriptures" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/SelfDecption.jpg", alt: "SelfDecption" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/StayByTheTree.jpg", alt: "StayByTheTree" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/TempleImage.jpg", alt: "TempleImage" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/Tent.jpg", alt: "Tent" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/The Circle of the Cities.gif", alt: "The Circle of the Cities" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/TheFakeBeat.jpg", alt: "TheFakeBeat" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/TheFourJosephs.jpg", alt: "TheFourJosephs" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/throne_complete.jpg", alt: "throne_complete" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/Trail-of-Blood-chart-combined.png", alt: "Trail-of-Blood-chart-combined" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/TrickEyeMuseum1.jpg", alt: "TrickEyeMuseum1" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/TrickEyeMuseum2.jpg", alt: "TrickEyeMuseum2" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/TunbridgeVermont.gif", alt: "TunbridgeVermont" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/two_hour_block_graph.jpeg", alt: "two_hour_block_graph" },
    { url: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/Valley.jpg", alt: "Valley" },
];


export default {
    title: "editor/HtmlEditor",
    component: HtmlEditor,
    argTypes: {
        onSave: {
            description: "Event raised when the \"Save\" button is clicked.",
            action: "saved",
            table: {
                type: { summary: "function(htmlContent: string)" }
            }
        },
        initialHtml: {
            description: "The initial HTML content to be loaded into the editor.",
            control: "text",
            table: {
                type: { summary: "string" },
                defaultValue: { summary: "\"\"" }
            }
        },
        postURL: {
            description: "URL endpoint for image uploads.",
            control: "text",
            table: {
                type: { summary: "string" },
                defaultValue: { summary: "null" }
            }
        },
        styleClasses: {
            description: "CSS class names to expose in the Styles dropdown (applied as span classes).",
            control: { type: "object" },
            table: {
                type: { summary: "string[]" },
                defaultValue: { summary: "[]" }
            }
        }
    }
};

const Template = (args) => {
    const [content, setContent] = useState(args.initialHtml);

    const handleEditorChange = (newContent) => {
        setContent(newContent);
        action("content-changed")(newContent);
    };

    const imagesUploadHandler = (blobInfo, success, failure) => {
        return "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/images/Valley.jpg";
    };

    const handleSave = () => {
        action("saved")(content);
        if (args.onSave) {
            args.onSave(content);
        }
    };

    return (
        <HtmlEditor
            {...args}
            images={images}
            initialHtml={content}
            onContentChanged={handleEditorChange}
            onSave={handleSave}
            imagesUploadHandler={imagesUploadHandler}
        />
    );
};

export const Default = Template.bind({});
Default.args = {
    initialHtml: "<div class=\"comingSoon\">Coming Soon...</div>",
    postURL: "/uploadImage",
    cssfilePath: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/themes/theme.css"
};

Default.storyName = "Default HtmlEditor";
Default.parameters = {
    docs: {
        storyDescription: `
The HtmlEditor is a versatile component built on top of the TinyMCE library. It provides a WYSIWYG interface for content creation, along with a source view for direct HTML editing.

**Features**:
- Edit and preview modes
- Source code viewing
- Image insertion (with configurable upload endpoint via the \`postURL\` prop)
- Resizable editor (with a minimum height of 200px)
- Built-in Save functionality (triggers an onSave event with the current content)

**Usage**:
Simply incorporate the HtmlEditor component into your app and provide it with the necessary props for customization.`
    }
};

export const WithStyleClasses = Template.bind({});
WithStyleClasses.args = {
    initialHtml: "<p><strong>Select text</strong> and open the Styles dropdown to apply classes.</p>",
    cssfilePath: "https://www-projectdata00353-dev.s3.us-west-2.amazonaws.com/public/assets/ldsgospeldoctrine.info/themes/theme.css",
    styleClasses: [
        "intro__title",
        "brush-button",
        "hero__band",
        "HappyFont"
    ]
};
WithStyleClasses.storyName = "With styleClasses (Styles dropdown)";

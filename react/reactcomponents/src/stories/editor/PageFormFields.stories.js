
import React from "react";
import PageFormFields from "../../components/editor/PageFormFields";
import imageList from "../constants/imageList";
import { action } from "@storybook/addon-actions";




const keywordOptions = [
    "Wives",
    "African-American",
    "Heavenly-Mother",
    "Kingdoms-of-Glory",
    "Exaltation"
];
const topicsOptions = [
    "Polygamy",
    "Blacks in Priesthood",
    "Heavenly Mother",
    "Kingdoms of Glory",
    "Exaltation"
];

const columns = [
    { field: "name", Header: "Name", Type: "Text" },
    { field: "description", Header: "Description", Type: "Text" },
    { field: "topics", Header: "Topics", Type: "Tags" },
    { field: "keywords", Header: "Keywords", Type: "Tags" },
    { field: "articleId", Header: "Article Id", Type: "Text" },
    { field: "memeImagePath", Header: "Meme Image Path", Type: "Text" },
    { field: "articlePath", Header: "Article Path", Type: "Text" }
];
const uniqueID = () => {
    return Date.now();
};
const currentPage = {
    "name": "page 1",
    "description": "page Description 1",
    "topics": ["sample Page Topic1", "sample Page Topic2"],
    "keywords": ["sample Page Keyword 1", "sample Page Keyword 2"],
    "style": "Blue Style",
    "layout": "Standard",
    "articles": [
        {
            "id": 1,
            "articleId": "SampleID",
            "name": "SampleName",
            "description": "SampleDescription",
            "topics": ["sampleTopic1", "sampleTopic2", "sampleTopic3", "sampleTopic4"],
            "keywords": ["sampleKeyword1", "sampleKeyword2"],
            "memeImagePath": "",
            "articlePath": "gospeldoctrine_info_home.html"
        },
        {
            "id": 2,
            "articleId": "SampleID1",
            "name": "SampleName",
            "description": "SampleDescription",
            "topics": ["sampleTopic1", "sampleTopic2", "sampleTopic3",],
            "keywords": ["sampleKeyword1", "sampleKeyword2"],
            "memeImagePath": "",
            "articlePath": "gospeldoctrine_footer.html"
        },
        {
            "id": 3,
            "articleId": "SampleID2",
            "name": "SampleName",
            "description": "SampleDescription",
            "topics": ["sampleTopic1", "sampleTopic2",],
            "keywords": ["sampleKeyword1", "sampleKeyword2"],
            "memeImagePath": "",
            "articlePath": "gospeldoctrine_footer1.html"
        },
        {
            "id": 4,
            "articleId": "SampleID3",
            "name": "SampleName",
            "description": "SampleDescription",
            "topics": ["sampleTopic1",],
            "keywords": ["sampleKeyword1",],
            "memeImagePath": "",
            "articlePath": "gospeldoctrine_footer1.html"
        }
    ]
};
const selectedLayout =  "Standard";



const styleSheets = [
    "Blue Style",
    "Gold Style",
    "Red Style",
    "Gray Style",
    "Bright Style"
];
export default {
    title: "editor/PageFormFields",
    component: PageFormFields,
    savePage: { action: "Save Button Clicked" },

};

const Template = (args) => <PageFormFieldsWrapper {...args} />;

const PageFormFieldsWrapper = (args) => {

    const handlePageChange = (field, value) => {
        console.log({ ...currentPage, [field]: value });
        if (field === "layout") {
            console.log(value);
        }
    };


    return (
        <div>
            <PageFormFields {...args} imageList={imageList} uniqueID={uniqueID} onPageChange={handlePageChange} />
        </div>
    );
};
export const DefaultSearchGrid = Template.bind({});
DefaultSearchGrid.args = {
    selectedLayout: selectedLayout,
    currentPage: currentPage,
    styleSheets: styleSheets,
    keywordOptions: keywordOptions,
    topicsOptions: topicsOptions,
    onSave: (data) => {
        console.log("newArticle");
        action("onSave")(data);
    },
};

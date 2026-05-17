/// <reference path="../../components/editor/pageselector.jsx" />
/// <reference path="../../components/editor/createeditpage.jsx" />
/// <reference path="../../components/editor/createeditpage.jsx" />
import React, { useState } from "react";
import CreateEditPage from "../../components/editor/CreateEditPage";
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
    { field: "name", Header: "Name", Type: "Text"},
    { field: "description", Header: "Description", Type: "Text"},
    { field: "topics", Header: "Topics", Type: "Tags"},
    { field: "keywords", Header: "Keywords", Type: "Tags"},
    { field: "articleId", Header: "Article Id", Type: "Text"},
    { field: "memeImagePath", Header: "Meme Image Path", Type: "Text"},
    { field: "articlePath", Header: "Article Path", Type: "Text"}
];
const uniqueID = () => {
    return Date.now();
};
const pageData = {
    "name": "page 1",
    "description": "page Description 1",
    "topics": ["sample Page Topic1", "sample Page Topic2"],
    "keywords": ["sample Page Keyword 1", "sample Page Keyword 2"],
    "style": "Blue Style",
    "layout":"Standard",
    "articles": [
        {
            "id": 1,
            "articleId": "SampleID",
            "name": "SampleName 1",
            "description": "SampleDescription",
            "topics": ["sampleTopic1", "sampleTopic2", "sampleTopic3", "sampleTopic4"],
            "keywords": ["sampleKeyword1", "sampleKeyword2"],
            "memeImagePath": "",
            "articlePath": "gospeldoctrine_info_home.html"
        },
        {
            "id": 5,
            "articleId": "SampleID1",
            "name": "SampleName 2",
            "description": "SampleDescription",
            "topics": ["sampleTopic1", "sampleTopic2", "sampleTopic3",],
            "keywords": ["sampleKeyword1", "sampleKeyword2"],
            "memeImagePath": "",
            "articlePath": "gospeldoctrine_footer.html"
        },
        {
            "id": 3,
            "articleId": "SampleID2",
            "name": "SampleName 3",
            "description": "SampleDescription",
            "topics": ["sampleTopic1", "sampleTopic2", ],
            "keywords": ["sampleKeyword1", "sampleKeyword2"],
            "memeImagePath": "",
            "articlePath": "gospeldoctrine_footer1.html"
        },
        {
            "id": 4,
            "articleId": "SampleID3",
            "name": "SampleName 4",
            "description": "SampleDescription",
            "topics": ["sampleTopic1", ],
            "keywords": ["sampleKeyword1",],
            "memeImagePath": "",
            "articlePath": "gospeldoctrine_footer1.html"
        }
    ]
};
const styleSheets = [
    "Blue Style",
    "Gold Style",
    "Red Style",
    "Gray Style",
    "Bright Style"
];
export default {
    title: "editor/CreateEditPage",
    component: CreateEditPage,
    savePage: { action: "Save Button Clicked" },

};

const Template = (args) => <CreateEditPageWrapper {...args} />;


const CreateEditPageWrapper = (args) => {
    const [maxId, setMaxId] = useState(5);
    const handleArticleUpdated = async (input) => {
        return new Promise((resolve) => {
            setTimeout(() => {
                if (!input.id) {
                    input.id = maxId + 1;
                    setMaxId(input.id);
                }
                resolve(input);
            }, 1000); // Simulate async call with 1 second delay
        });
    };
    return (
        <div>
            <CreateEditPage {...args} imageList={imageList} uniqueID={uniqueID} onArticleUpdated={handleArticleUpdated} />
        </div>
    );
};

export const DefaultSearchGrid = Template.bind({});
DefaultSearchGrid.args = {
    pageData: pageData,
    styleSheets: styleSheets,
    columns: columns,
    keywordOptions: keywordOptions,
    topicsOptions: topicsOptions,
    pageSize:[2,5,10,20,50,100],
    onSave: (data) => {
        console.log("newArticle");
        action("onSave")(data);
    },
};

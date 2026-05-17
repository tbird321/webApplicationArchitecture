import React from "react";
import { action } from "@storybook/addon-actions";

import ArticleModal from "../../components/editor/ArticleModal";
import imageList from "../constants/imageList";


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
            "articleId": "SampleID1",
            "name": "SampleName",
            "description": "SampleDescription",
            "topics": ["sampleTopic1", "sampleTopic2", ],
            "keywords": ["sampleKeyword1", "sampleKeyword2"],
            "memeImagePath": "",
            "articlePath": "gospeldoctrine_footer1.html"
        },
        {
            "id": 4,
            "articleId": "SampleID1",
            "name": "SampleName",
            "description": "SampleDescription",
            "topics": ["sampleTopic1", ],
            "keywords": ["sampleKeyword1",],
            "memeImagePath": "",
            "articlePath": "gospeldoctrine_footer1.html"
        }
    ]
};

const handleSetArticlesLoadMock = (newArticles) => {
    console.log("Updated articles:", newArticles);
    // Additional logic for Storybook, if needed
};

export default {
    title: "editor/ArticleModal",
    component: ArticleModal,
};

const Template = (args) => <ArticleModal {...args}  imageList={imageList} uniqueID = {uniqueID} articlesLoad={pageData.articles} handleSetArticlesLoad={handleSetArticlesLoadMock}/>;

export const DefaultSearchGrid = Template.bind({});
DefaultSearchGrid.args = {
    keywordOptions: keywordOptions,
    topicsOptions: topicsOptions,
    pageSize:[2,5,10,20,50,100],
    onSave: (data) => {
        console.log("newArticle");
        action("onSave")(data);
    },
};

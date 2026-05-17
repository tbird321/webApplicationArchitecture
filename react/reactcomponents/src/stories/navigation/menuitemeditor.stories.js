import React, { useState, useEffect } from "react";
import MenuEditor from "../../components/navigation/MenuEditor"; // Adjust the import path accordingly
import Button from "@mui/material/Button";

export default {
    title: "Navigation/MenuEditor",
    component: MenuEditor,
};
const page1 = {
    "id": "page1",
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
const page2 = {
    "id": "page2",
    "name": "page 2",
    "description": "page Description 2",
    "topics": ["sample Page Topic1", "sample Page Topic2"],
    "keywords": ["sample Page Keyword 1", "sample Page Keyword 2"],
    "style": "Gold Style",
    "layout": "Standard",
    "articles": [
        {
            "id": 1,
            "articleId": "SampleID7",
            "name": "SampleName",
            "description": "SampleDescription",
            "topics": ["sampleTopic1", "sampleTopic2", "sampleTopic3", "sampleTopic4"],
            "keywords": ["sampleKeyword1", "sampleKeyword2"],
            "memeImagePath": "",
            "articlePath": "gospeldoctrine_info_home.html"
        },
        {
            "id": 2,
            "articleId": "SampleID8",
            "name": "SampleName",
            "description": "SampleDescription",
            "topics": ["sampleTopic1", "sampleTopic2", "sampleTopic3",],
            "keywords": ["sampleKeyword1", "sampleKeyword2"],
            "memeImagePath": "",
            "articlePath": "gospeldoctrine_footer.html"
        },
        {
            "id": 3,
            "articleId": "SampleID9",
            "name": "SampleName",
            "description": "SampleDescription",
            "topics": ["sampleTopic1", "sampleTopic2",],
            "keywords": ["sampleKeyword1", "sampleKeyword2"],
            "memeImagePath": "",
            "articlePath": "gospeldoctrine_footer1.html"
        },
        {
            "id": 4,
            "articleId": "SampleID10",
            "name": "SampleName",
            "description": "SampleDescription",
            "topics": ["sampleTopic1",],
            "keywords": ["sampleKeyword1",],
            "memeImagePath": "",
            "articlePath": "gospeldoctrine_footer1.html"
        }
    ]
};
const pages = [page1, page2];
// Sample JSON data
const sampleMenuObjectOchestration = [
    {
        id: "menu1",
        text: "Menu 1",
        pageName: "Tree of Life",
        articleName: null,
        pageId: 1, // or the ID of the linked page
        articleId: null,
        menuItems: [
            {
                id: "item.1",
                text: "Item 1.1",
                pageName: "Tree of Life",
                articleName: null,
                pageId: 1, // or the ID of the linked page
                articleId: null,
                menuItems: [],
            },
            {
                id: "item.2",
                text: "Item 1.2",
                pageName: "Tree of Life",
                articleName: null,
                pageId: 1,
                articleId: null,
                menuItems: [],
            },
        ],
    },
    {
        id: "menu 2",
        text: "Menu 2",
        pageName: "Tree of Life",
        articleName: null,
        pageId: 1,
        articleId: null,
        menuItems: [
            {
                id: "item2.1",
                text: "Item 2.1",
                pageName: "Tree of Life",
                pageId: 1,
                articleId: null,
                menuItems: [
                    {
                        id: "deepItem2.1.1",
                        text: "Deep Item 2.1.1",
                        pageName: "Tree of Life",
                        articleName: null,
                        pageId: 1,
                        articleId: null,
                        menuItems: [],
                    },
                    {
                        id: "deepItem2.1.2",
                        text: "Deep Item 2.1.2",
                        pageName: "Tree of Life",
                        articleName: null,
                        pageId: 1,
                        articleId: null,
                        menuItems: [],
                    },
                ],
            },
            {
                id: "item2.2",
                text: "Item 2.2",
                pageName: "Tree of Life",
                articleName: null,
                pageId: 1,
                articleId: null,
                menuItems: [],
            },
        ],
    },
    {
        id: "menu3",
        text: "Menu 3",
        pageName: "Tree of Life",
        articleName: null,
        pageId: 1,
        articleId: null,
        menuItems: [],
    },
];
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
const sampleMenuObject = [
    {
        id: "menu1",
        text: "Menu 1",
        menuItems: [
            {
                id: "item.1",
                text: "Item 1.1",
            },
            {
                id: "item.2",
                text: "Item 1.2",
            },
        ],
    },
    {
        id: "menu 2",
        text: "Menu 2",
        menuItems: [
            {
                id: "item2.1",
                text: "Item 2.1",
                menuItems: [
                    {
                        id: "deepItem2.1.1",
                        text: "Deep Item 2.1.1",
                        menuItems: [],
                    },
                    {
                        id: "deepItem2.1.2",
                        text: "Deep Item 2.1.2",
                        menuItems: [],
                    },
                ],
            },
            {
                id: "item2.2",
                text: "Item 2.2",
                menuItems: [],
            },
        ],
    },
    {
        id: "menu3",
        text: "Menu 3",
        menuItems: [],
    },
];

const itemsUpdated = (updatedItems) => {
    console.log(updatedItems);
};
const onSearch = (data) => {

    //make database call instead

};

const generateColumns = (columns) => {
    const columnDefinitions = columns?.map(column => {
        let columnDef = {
            field: column.field,
            headerName: column.Header,
            flex: 1.5,
            type: column.Type,
        };
        return columnDef;
    });

    return columnDefinitions;
};
const dataGridColumns =
    [
        { field: "name", Header: "Name", Type: "Text" },
        { field: "description", Header: "Description", Type: "Text" },
        { field: "topics", Header: "Topics", Type: "Tags" },
        { field: "keywords", Header: "Keywords", Type: "Tags" },
    ];


const Template = (args) => <MenuEditor {...args} />;

export const Default = Template.bind({});
Default.args = {
    menuItems: sampleMenuObject,
    onItemsUpdated: itemsUpdated,
    pages: pages,
    onSearch: onSearch,
    pageSize: [2, 5, 10, 20, 50, 100],
    dataGridColumns: dataGridColumns,
    keywordOptions: keywordOptions,
    topicsOptions: topicsOptions,
    // Provide initial arguments for your MenuEditor component if needed
};

Default.storyName = "Menu Editor";

const TemplateOchestration = (args) =>

    <>
        <MenuEditor {...args} />
    </>;

export const Orchestration = TemplateOchestration.bind({});
Orchestration.args = {
    menuItems: sampleMenuObjectOchestration,
    onItemsUpdated: itemsUpdated,
    pages: pages,
    onSearch: onSearch,
    pageSize: [2, 5, 10, 20, 50, 100],
    dataGridColumns: dataGridColumns,
    keywordOptions: keywordOptions,
    topicsOptions: topicsOptions,

    // Provide initial arguments for your MenuEditor component if needed
};

Orchestration.storyName = "Orchestration";




import React, {useState} from "react";
import PageSelector from "../../components/editor/PageSelector";
import { ModalDialog } from "../../components";
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
const layoutsOptionA = [
    "Standard",
    "2 Grid",
    "3 Grid",
    "1-2 Grid",
    "1-3 Grid",
    "1/3 2/3 Grid",
    "2/3 1/3 Grid",
    "1 Row 1/3 2/3 Row",
    "1 Row 2/3 1/3 Row",
    "1 Row 3 Row",
    "1 Row 2 Row 3 Row",
    "2 Row 3 Row"
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
const page1 = {
    "id":"page1",
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
            "articleId": "SampleID2",
            "name": "SampleName",
            "description": "SampleDescription",
            "topics": ["sampleTopic1", "sampleTopic2", ],
            "keywords": ["sampleKeyword1", "sampleKeyword2"],
            "memeImagePath": "",
            "articlePath": "gospeldoctrine_footer1.html"
        },
        {
            "id": 4,
            "articleId": "SampleID3",
            "name": "SampleName",
            "description": "SampleDescription",
            "topics": ["sampleTopic1", ],
            "keywords": ["sampleKeyword1",],
            "memeImagePath": "",
            "articlePath": "gospeldoctrine_footer1.html"
        }
    ]
};
const page2 = {
    "id":"page2",
    "name": "page 2",
    "description": "page Description 2",
    "topics": ["sample Page Topic1", "sample Page Topic2"],
    "keywords": ["sample Page Keyword 1", "sample Page Keyword 2"],
    "style": "Gold Style",
    "layout":"Standard",
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
            "topics": ["sampleTopic1", "sampleTopic2", ],
            "keywords": ["sampleKeyword1", "sampleKeyword2"],
            "memeImagePath": "",
            "articlePath": "gospeldoctrine_footer1.html"
        },
        {
            "id": 4,
            "articleId": "SampleID10",
            "name": "SampleName",
            "description": "SampleDescription",
            "topics": ["sampleTopic1", ],
            "keywords": ["sampleKeyword1",],
            "memeImagePath": "",
            "articlePath": "gospeldoctrine_footer1.html"
        }
    ]
};
const pages=[page1,page2];


const styleSheets = [
    "Blue Style",
    "Gold Style",
    "Red Style",
    "Gray Style",
    "Bright Style"
];
export default {
    title: "editor/PageSelector",
    component: PageSelector,
    savePage: { action: "Save Button Clicked" },

};

const Template = (args) => <PageSelectorWrapper {...args} />;



const PageSelectorWrapper = (args) => {
    const [pageData, setPageData]= useState([]);
    const handleSearch =(searchCrit)=>{
        setPageData(pages);
        return pages;
    };
    const handleSavePages=(tempPages)=>{
        console.log(tempPages);
    };

    const handleArticleSearch=(searchCrit)=>{
        console.log(searchCrit);
        return [
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
                "topics": ["sampleTopic1", "sampleTopic2", ],
                "keywords": ["sampleKeyword1", "sampleKeyword2"],
                "memeImagePath": "",
                "articlePath": "gospeldoctrine_footer1.html"
            },
            {
                "id": 4,
                "articleId": "SampleID3",
                "name": "SampleName",
                "description": "SampleDescription",
                "topics": ["sampleTopic1", ],
                "keywords": ["sampleKeyword1",],
                "memeImagePath": "",
                "articlePath": "gospeldoctrine_footer1.html"
            }
        ];
    };

    return (
        <div>
            <PageSelector {...args} imageList={imageList} pages={pageData} onSearch={handleSearch} onSavePage ={handleSavePages} onArticleSearch={handleArticleSearch}/>
        </div>
    );
};

export const DefaultSearchGrid = Template.bind({});
DefaultSearchGrid.args = {
    styleSheets: styleSheets,
    layouts:layoutsOptionA,
    articleColumns: columns,
    keywordOptions: keywordOptions,
    topicsOptions: topicsOptions,
    pageSize: [2, 5, 10, 20, 50, 100],
    onSave: (data) => {
        console.log("tempPages");
        action("onSave")(data);
    },
};

const ModalTemplate = (args) =><ModalDialog open={true} onClose={() => {  }}>
    <PageSelectorModalWrapper {...args} />
</ModalDialog>;


const PageSelectorModalWrapper = (args) => {
    const handleSavePages=(tempPages)=>{
        console.log(tempPages);
    };
    const [pageData, setPageData]= useState([]);
    const handleSearch =(searchCrit)=>{
        console.log(searchCrit);
        return pages;
    };
    const handleArticleSearch=(searchCrit)=>{
        console.log(searchCrit);
        return [
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
            }];
    };
    return (
        <div>
            <PageSelector {...args} pages={pageData} imageList={imageList} onSearch={handleSearch} handleSavePages ={handleSavePages} onArticleSearch={handleArticleSearch}/>
        </div>
    );
};

export const ModalSearchGrid = ModalTemplate.bind({});
ModalSearchGrid.args = {
    styleSheets: styleSheets,
    layouts:layoutsOptionA,
    articleColumns: columns,
    keywordsOptions: keywordOptions,
    topicsOptions: topicsOptions,
    pageSize: [2, 5, 10, 20, 50, 100],
    onSave: (data) => {
        console.log("tempPages");
        action("onSave")(data);
    },
};

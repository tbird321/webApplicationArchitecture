import React, { useState } from "react";
import SearchGrid from "../../components/data/SearchGrid";

const allItems = [
    { id: 4, Name: "Polygamy and the Church", Description: "A doctrinal Discussion of Polygamy in the church", Keywords: "Wives", Topic: "Polygamy", Link: "www.Polygamy.org" },
    { id: 6, Name: "Blacks in the Priesthood", Description: "A doctrinal Discussion of Blacks in the Priesthood", Keywords: "African-American", Topic: "Blacks in Priesthood", Link: "www.BlacksInPriesthood.org" },
    { id: 8, Name: "Heavenly Mother", Description: "A doctrinal Discussion of Heavenly Mother", Keywords: "Heavenly-Mother", Topic: "Heavenly-Mother", Link: "www.HeavenlyMother.org" },
    { id: 11, Name: "Kingdoms of Glory", Description: "A doctrinal Discussion of Kingdoms of Glory", Keywords: "Kingdoms of Glory", Topic: "Kingdoms of Glory", Link: "www.KingdomsOfGlory.org"},
    { id: 54, Name: "Pre-Earth Life", Description: "A doctrinal Discussion of Pre-Earth Life", Keywords: "Pre-Earth Life", Topic: "Pre-Earth Life", Link: "www.PreEarthLife.org"},
    { id: 1, Name: "Exaltation", Description: "A doctrinal Discussion of Exaltation", Keywords: "Exaltation", Topic: "Exaltation", Link: "www.Exaltation.org"},
];

// Generate more data for pagination testing
const generateTestData = () => {
    const testData = [];
    const topics = ["Doctrine", "History", "Leadership", "Scripture", "Temple", "Family", "Missionary", "Education"];
    const keywords = ["Faith", "Repentance", "Baptism", "Holy Ghost", "Eternal Life", "Salvation", "Grace", "Mercy"];

    for (let i = 1; i <= 50; i++) {
        testData.push({
            id: i,
            Name: `Test Item ${i}`,
            Description: `This is a test description for item ${i}. It contains sample text to demonstrate the grid functionality.`,
            Keywords: keywords[Math.floor(Math.random() * keywords.length)],
            Topic: topics[Math.floor(Math.random() * topics.length)],
            Link: `www.testitem${i}.org`
        });
    }
    return testData;
};

const columns = [
    {field: "Name", headerName: "Name", width: 150, editable: true,},
    {field: "Description", headerName: "Description", width: 150, editable: true,},
    {field: "Keywords", headerName: "Keywords", width: 150, editable: true,},
    {field: "Topic", headerName: "Topic", width: 110, editable: true,},
    {field: "Link", headerName: "Link", width: 160, editable: true,},
];
const keywordOptions = [
    "Wives",
    "African-American",
    "Heavenly-Mother",
    "Kingdoms-of-Glory",
    "Exaltation",
    "Faith",
    "Repentance",
    "Baptism",
    "Holy Ghost",
    "Eternal Life",
    "Salvation",
    "Grace",
    "Mercy"
];
const topicsOptions = [
    "Polygamy",
    "Blacks in Priesthood",
    "Heavenly Mother",
    "Kingdoms of Glory",
    "Exaltation",
    "Doctrine",
    "History",
    "Leadership",
    "Scripture",
    "Temple",
    "Family",
    "Missionary",
    "Education"
];
export default {
    title: "Data/SearchGrid",
    component: SearchGrid,
};
const Template = (args) => <SearchGridWrapper {...args} />;

const SearchGridWrapper = (args) => {

    function fetchData() {
        return new Promise((resolve) => {
            setTimeout(() => {
                // After 1 second, resolve the promise with an array of data
                resolve(allItems);
            }, 1000); // 1000 milliseconds = 1 second
        });
    }

    const handleSearch = (criteria) => {
        return fetchData();
    };
    const handleSelect = (selectedItem) => {
        console.log(selectedItem);
    };

    return (
        <div>
            <SearchGrid {...args} onSearch={handleSearch} onRowSelect={handleSelect} />
        </div>
    );
};

export const DefaultSearchGrid = Template.bind({});
DefaultSearchGrid.args = {
    columns:columns,
    pageSize: [2, 5, 10, 20, 50, 100],
    topicsOptions: topicsOptions,
    keywordOptions: keywordOptions,
    items:[]
};

export const SearchGridWithData = Template.bind({});
SearchGridWithData.args = {
    columns: columns,
    pageSize: 10,
    pageSizeOptions: [5, 10, 25, 50],
    topicsOptions: topicsOptions,
    keywordOptions: keywordOptions,
    items: generateTestData(),
    searchType: "page"
};

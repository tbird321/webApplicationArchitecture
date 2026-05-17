import React from "react";
import StylingGrid from "../../components/styling/StylingGrid";
import imageList from "../constants/imageList";

const attColumns = [
    { field: "attName", Header: "Attribute Name", Type: "Text"},
    { field: "attValue", Header: "Attribute Value", Type: "Text"},
];
const attTypes = [
    {label: "Color", value: "color"},
    {label: "Size", value: "size"},
    {label: "Font", value: "font"},
    {label: "Overflow", value: "overflow"},
    {label: "Position", value: "position"},
    {label: "Repeat", value: "repeat"},
    {label: "Decoration", value: "decoration"},
    {label: "Border Style", value: "borderStyle"},
    {label: "Block Positon", value: "blockPosition"},
    {label: "Float", value: "float"},
    {label: "Vertical Align", value: "verticalAlign"},
    {label: "Visibility", value: "visibility"},
    {label: "List Style Type", value: "listStyleType"},
    {label: "Opacity", value: "opacity"},
    {label: "z Index", value: "zIndex"},
];

const selectors = [
    { "id": 1,
        "name": "Selector1",
        "type": "class",
        "styling Elements": [
            {
                "id": 1,
                "attName": "Color",
                "attValue": "#F33434",
                "attType": {label: "Color", value: "color"},
            }]
    },
    {
        "id": 2,
        "name": "Selector2",
        "type": "Id",
        "styling Elements": [
            {
                "id": 2,
                "attName": "Margin Top",
                "attValue": "15px",
                "attType": {label: "Size", value: "size"},
            }]
    },
];

export default {
    title: "styling/StylingGrid",
    component: StylingGrid,
    argTypes: {


    },
};

const currentSelector = {
    "id": 2,
    "name": "Selector2",
    "type": "Id",
    "stylingElements": [
        {
            "id": 1,
            "attName": "margin-top",
            "attValue": "15px",
            "attType": {label: "Size", value: "size"},
        }
    ]
};

const mockHandleSetCurrentSelector = (selector) => {
    console.log("Current Selector Updated:", selector);
};
const mockHandleUpdateSelectors = (selectorCopy) => {
    console.log("Selectors Updated:", selectorCopy);

};

const uniqueID = () => {
    return Date.now();
};

const Template = (args) => <StylingGrid {...args} imageList={imageList} onSetCurrentSelector={mockHandleSetCurrentSelector} onUpdateSelectors={mockHandleUpdateSelectors} currentSelector={currentSelector} attTypes={attTypes} uniqueID={uniqueID}/>;

export const DefaultLogin = Template.bind({});
DefaultLogin.args = {
    attColumns: attColumns,
    selectors: selectors,
    pageSize:[2,5,10,20,50,100],
};

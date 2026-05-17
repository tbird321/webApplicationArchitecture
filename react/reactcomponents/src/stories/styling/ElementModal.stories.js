import React from "react";
import ElementModal from "../../components/styling/ElementModal";
import { action } from "@storybook/addon-actions";


const attColumns = [
    { field: "attName", Header: "Attribute Name", Type: "Text"},
    { field: "attValue", Header: "Attribute Value", Type: "Text"},
    { field: "attType", Header: "Attribute Type", Type: "Text"},
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
const newStyleElement={
    "id": 1,
    "attName": "color",
    "attValue": "#EF0B0B",
    "attType": {label: "Color", value: "color"},
};
const uniqueID = () => {
    return Date.now();
};
export default {
    title: "styling/ElementModal",
    component: ElementModal,
    argTypes: {


    },
};

const handleNewStyleElementSave=(styleAttribute)=>{
    console.log(styleAttribute);
};

//newStyleElement,handleNewStyleElementSave,attTypes
const Template = (args) => <ElementModal {...args} uniqueID={uniqueID} />;

export const DefaultLogin = Template.bind({});
DefaultLogin.args = {

    onElementSave:action("Save Complete"),
    newStyleElement:newStyleElement,
    attColumns: attColumns,
    attTypes:attTypes,
    handleNewStyleElementSave:handleNewStyleElementSave
};

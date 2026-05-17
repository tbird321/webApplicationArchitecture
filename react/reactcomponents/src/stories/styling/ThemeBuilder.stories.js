import React from "react";
import ThemeBuilder from "../../components/styling/ThemeBuilder";
import { action } from "@storybook/addon-actions";
import { ModalDialog } from "../../components";
const columns = [
    { field: "name", Header: "Name", Type: "Text"},
    { field: "type", Header: "Type", Type: "Text"},
];
const selectors = [
    { "id": 1,
        "name": "Selector1",
        "type": {label: "Class", value: "class"},
        "stylingElements": [
            {
                "id": 1,
                "attName": "color",
                "attValue": "#E50C0C",
                "attType": {label: "Color", value: "color"},
            }]
    },
    {
        "id": 2,
        "name": "Selector2",
        "type": {label: "Id", value: "id"},
        "stylingElements": [
            {
                "id": 1,
                "attName": "margin-top",
                "attValue": "15px",
                "attType": {label: "Size", value: "size"},
            }]
    },
];

export default {
    title: "styling/ThemeBuilder",
    component: ThemeBuilder,
    argTypes: {


    },
};

const Template = (args) =><div style={{width:"1200px",height:"600px",paddingLeft:"50px"}}> <ThemeBuilder {...args} /></div>;

export const DefaultRender = Template.bind({});
DefaultRender.args = {
    columns: columns,
    selectors: selectors,
    pageSize: [2, 5, 10, 20, 50, 100],
    onSaveSelectors: (data) => {
        console.log("Selectors:", data);
        action("onSaveSelectors")(data);
    },
};

const ModalTemplate = (args) =><ModalDialog open={true} onClose={() => {  }}>

    <ThemeBuilder {...args} />
</ModalDialog>;

export const WithinModal = ModalTemplate.bind({});
WithinModal.args = {
    columns: columns,
    selectors: selectors,
    pageSize: [2, 5, 10, 20, 50, 100],
    onSaveSelectors: (data) => {
        console.log("Selectors:", data);
        action("onSaveSelectors")(data);
    },
};

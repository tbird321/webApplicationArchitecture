import React from "react";
import TemplateGrid from "../../components/editor/TemplateGrid";
import { action } from "@storybook/addon-actions";
import { ModalDialog } from "../../components";
const columns = [
    { field: "title", Header: "Title", Type: "Text"},
    { field: "description", Header: "Description", Type: "Text"},
    { field: "content", Header: "Content", Type: "Text"},
];
const templates = [
    {
        "id": 1,
        "title": "Wrapping div",
        "description": "add a div with content",
        "content": "<div>This is some sample code to add</div>"
    },
    {
        "id": 2,
        "title": "Wrapping div2",
        "description": "add a div with content",
        "content": "<div>This is some sample code to add</div>"
    },
    {
        "id": 3,
        "title": "Wrapping div3",
        "description": "add a div with content",
        "content": "<div>This is some sample code to add</div>"
    },

];
export default {
    title: "editor/TemplateGrid",
    component: TemplateGrid,
    argTypes: {


    },
};

const Template = (args) =><><TemplateGrid {...args}/></>;

export const DefaultRender = Template.bind({});
DefaultRender.args = {
    columns: columns,
    templates: templates,
    pageSize:[2,5,10,20,50,100],
    onSaveTemplates:action("Templates Saved"),
};

const ModalTemplate = (args) =>
    <ModalDialog open={true} onClose={() => {  }}>
        <TemplateGrid {...args} />
    </ModalDialog>;

export const WithinModal = ModalTemplate.bind({});
WithinModal.args = {
    columns: columns,
    templates: templates,
    pageSize:[2,5,10,20,50,100],
    onSaveTemplates: (data)=> {
        alert("save fired");
        action(data);
    },
};

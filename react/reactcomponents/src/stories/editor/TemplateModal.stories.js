import React from "react";
import TemplateModal from "../../components/editor/TemplateModal";
import { action } from "@storybook/addon-actions";
import { ModalDialog } from "../../components";

const currentTemplate = {
    "id": 1,
    "title": "Wrapping div",
    "description": "add a div with content",
    "content": "<div>This is some sample code to add</div>"
};
const editMode = true;
export default {
    title: "editor/TemplateModal",
    component: TemplateModal,
    argTypes: {


    },
};
const Template = (args) =><><TemplateModal {...args}/></>;

export const DefaultRender = Template.bind({});
DefaultRender.args = {
    editMode: editMode,
    currentTemplate: currentTemplate,
    pageSize:[2,5,10,20,50,100],
    onTemplateSave: action("Save Complete"),
    onIsDirty: action("onIsDirty checked"),

};

const ModalTemplate = (args) =>
    <ModalDialog open={true} onClose={() => {  }}>
        <TemplateModal {...args} />
    </ModalDialog>;

export const WithinModal = ModalTemplate.bind({});
WithinModal.args = {
    handleTemplateSave:action("Save Complete"),
    currentTemplate:currentTemplate,
    pageSize:[2,5,10,20,50,100],

};

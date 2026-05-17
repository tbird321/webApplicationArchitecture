import SelectorModal from "../../components/styling/SelectorModal";

const selectorTypeOptions = [
    {label: "Class", value: "class"},
    {label: "Id", value: "id"},
    {label: "Tag", value: "tag"},
];

export default {
    title: "styling/SelectorModal",
    component: SelectorModal,
    // Define argTypes if you want to control the types or descriptions of your props
    argTypes: {
        currentSelector: {
            description: "The current selector being edited or added",
            control: "object",
        },
        uniqueID: {
            description: "Function to generate a unique ID",
            action: "uniqueID",
        },
        handleSetEditMode: {
            description: "Function to handle setting edit mode",
            action: "handleSetEditMode",
        },
        editMode: {
            description: "Flag to indicate if the modal is in edit mode",
            control: "boolean",
        }
    }
};
const saveSelector = (selector) => console.log("Selector Saved:", selector);
const onSetEdit = () => console.log("Edit Mode");

const Template = (args) => <SelectorModal {...args} saveSelector={saveSelector} onSetEdit={onSetEdit}/>;

export const Default = Template.bind({});
Default.args = {
    currentSelector: { "id": null, "name": "", "type": "", "stylingElements": [] },
    selectorTypeOptions:selectorTypeOptions,
    editMode: false,
    uniqueID: () => Date.now(),
    isOpen: true // Always open
};

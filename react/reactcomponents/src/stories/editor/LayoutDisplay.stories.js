import React from "react";
import LayoutDisplay from "../../components/editor/LayoutDisplay";
import oneTwothirdOnethird from "../../components/images/1_2-3_1-3.png";
import { action } from "@storybook/addon-actions";

const currentLayout="1, 1/3, 2/3 Grid";
const uniqueID = () => {
    return Date.now();
};
export default {
    title: "editor/LayoutDisplay",
    component: LayoutDisplay,
    argTypes: {
    },
};
const Template = (args) => <LayoutDisplay {...args}/>;
export const DefaultLayoutDisplay = Template.bind({});
DefaultLayoutDisplay.args = {
    currentLayout:currentLayout,
};

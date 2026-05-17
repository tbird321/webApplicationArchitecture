import React from "react";
import Spinner from "../../components/styling/Spinner";
export default {
    title: "styling/Spinner",
    component: Spinner,
    argTypes: {},
};
const Template = (args) => <Spinner showSpinner={true} />;
export const DefaultLogin = Template.bind({});
DefaultLogin.args = {};

import React from "react";
import SignUp from "../../components/authorization/SignUp";

export default {
    title: "Authorization/SignUp",
    component: SignUp,
    argTypes: {
        onComponentChange: { action: "navigation button clicked" },
        onRegister: { action: "Register button clicked" },
    },
};

const Template = (args) => <SignUp {...args} />;

export const DefaultLogin = Template.bind({});
DefaultLogin.args = {
};

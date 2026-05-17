import React from "react";
import ResetPassword from "../../components/authorization/ResetPassword";

export default {
    title: "Authorization/ResetPassword",
    component: ResetPassword,
    argTypes: {
        onChangeAccountPassword: { action: "Change Password Button Clicked" },
    },
};

const Template = (args) => <ResetPassword {...args} />;

export const DefaultVerificationCode = Template.bind({});
DefaultVerificationCode.args = {};

export const WithCallbacks = Template.bind({});
WithCallbacks.args = {};

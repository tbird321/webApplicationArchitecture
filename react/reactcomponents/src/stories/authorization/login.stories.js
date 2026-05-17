import React from "react";
import Login from "../../components/authorization/Login";
import SignUp from "../../components/authorization/SignUp";


const username = "Fred";
export default {
    title: "Authorization/Login",
    component: Login,
    argTypes: {
        onComponentChange: { action: "navigation button clicked" },
        onLoginClick: { action: "login button clicked" },
    },
};

const Template = (args) => <Login {...args} />;

export const DefaultLogin = Template.bind({});
DefaultLogin.args = {
    username:username,
};

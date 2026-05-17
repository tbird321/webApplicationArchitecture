import React, { useState } from "react";
import Login from "../../components/authorization/Login";
import SignUp from "../../components/authorization/SignUp";
import VerificationCode from "../../components/authorization/VerificationCode";
import ForgotPasswordSubmit from "../../components/authorization/ForgotPasswordSubmit";
import ForgotPasswordRequest from "../../components/authorization/ForgotPasswordRequest";

export default {
    title: "Authorization/FullPackage",
    component: Login,
    argTypes: {
        onComponentChange: { action: "navigation button clicked" },
        onLoginClick: { action: "login button clicked" },
    },
};


function FullPackage() {

    const [Component, setComponent] = useState("Login");

    const handleComponentChange = (component) => {
        console.log(component);
        setComponent(component);
    };

    const handleLogin = async (username, password) => {
        console.log("Login Button Clicked");
    };

    const handleRegister = () => {

    };
    const handleVerify = () => {

    };
    const handleForgotPasswordRequest = () => {

    };

    const handleForgotPasswordSubmit = () => {
        alert("forget password workflow");
    };
    let signUpEnabled = "true";
    return (

        <div>
            {Component === "Login" && <Login signUpEnabled={signUpEnabled} onLoginClick={handleLogin} onComponentChange={handleComponentChange}></Login>}
            {Component === "SignUp" && <SignUp onRegister={handleRegister} onComponentChange={handleComponentChange}></SignUp>}
            {Component === "VerificationCode" && <VerificationCode onVerify={handleVerify} onResend={handleRegister} onComponentChange={handleComponentChange}></VerificationCode>}
            {Component === "ForgotPasswordRequest" && <ForgotPasswordRequest onSend={handleForgotPasswordRequest} onComponentChange={handleComponentChange}></ForgotPasswordRequest>}
            {Component === "ForgotPasswordSubmit" && <ForgotPasswordSubmit onResetPassword={handleForgotPasswordSubmit} onComponentChange={handleComponentChange}></ForgotPasswordSubmit>}
        </div>
    );
}
const TemplateWrapper = (args) => {

    return (
        <FullPackage {...args} />
    );
};

export const Default = TemplateWrapper.bind({});
Default.args = {};
Default.storyName = "Default FullPackage";

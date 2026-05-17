import React from "react";
import VerificationCode from "../../components/authorization/VerificationCode";

export default {
    title: "Authorization/VerificationCode",
    component: VerificationCode,
    argTypes: {
        onVerify: { action: "verify button clicked" },
        onResend: { action: "resend code button clicked" },
        onComponentChange: { action: "navigation button clicked" },
    },
};

const Template = (args) => <VerificationCode {...args} />;

export const DefaultVerificationCode = Template.bind({});
DefaultVerificationCode.args = {};

export const WithCallbacks = Template.bind({});
WithCallbacks.args = {
    onVerify: (code) => {
        console.log(`Verifying with code: ${code}`);
    },
    onResend: () => {
        console.log("Resend code button clicked");
    },
};

import React from "react";
import DynamicMenu from "../../components/navigation/DynamicMenu"; // Adjust the import path accordingly
import { action } from "@storybook/addon-actions";

export default {
    title: "Navigation/DynamicMenu",
    component: DynamicMenu,
    argTypes: {
    // Define your argTypes here if needed
    },
};

const Template = (args) => <DynamicMenu {...args} />;

const parentMenuItem = {
    id:"menu1",
    text: "Dashboard",
    menuItems: [
        {
            id:"menuItem1",
            text: "Profile",
            onClick: (item) => {
                alert(`Clicked ${item.text}`);
                // Add your logic for handling the "Profile" click here
            },
        },
        {
            id:"menuItem2",
            text: "Account Settings",
            onClick: (item) => {
                alert(`Clicked ${item.text}`);
                // Add your logic for handling the "Account Settings" click here
            },
        },
        {
            id:"menuItem3",
            text: "Logout",
            onClick: (item) => {
                alert(`Clicked ${item.text}`);
                // Add your logic for handling the "Logout" click here
            },
        },
    ],
};


const actionMenuItem = {
    id:"menu1",
    text: "Dashboard",
    menuItems: [
        {
            id:"menuItem1",
            text: "Profile",
            onClick: action("Profile Clicked"), // Use the action function to simulate the click event
        },
        {
            id:"menuItem2",
            text: "Account Settings",
            onClick: action("Account Settings Clicked"), // Simulate the click event
        },
        {
            id:"menuItem3",
            text: "Logout",
            onClick: action("Logout Clicked"), // Simulate the click event
        },
    ],
};

export const DefaultWithActions = Template.bind({});
DefaultWithActions.args = {
    parentMenuItem: actionMenuItem,
    // Provide initial arguments for your DynamicMenu component if needed
};


export const Default = Template.bind({});
Default.args = {
    parentMenuItem: parentMenuItem,
    // Provide initial arguments for your DynamicMenu component if needed
};

DefaultWithActions.parameters = {
    docs: {
        storyDescription: `
The DynamicMenu component provides a dynamic menu that opens and closes when clicking a button. It can be used for creating menus like user profiles, account settings, and logout.

**Features**:
- Dynamic menu that opens and closes
- Customizable menu items

**Usage**:
Simply incorporate the DynamicMenu component into your app and provide it with the necessary props for customization.`
    },
};

Default.storyName = "Dynamic Menu with Alerts";
Default.parameters = {
    docs: {
        storyDescription: `
The DynamicMenu component provides a dynamic menu that opens and closes when clicking a button. It can be used for creating menus like user profiles, account settings, and logout.

**Features**:
- Dynamic menu that opens and closes
- Customizable menu items

**Usage**:
Simply incorporate the DynamicMenu component into your app and provide it with the necessary props for customization.`
    },
};

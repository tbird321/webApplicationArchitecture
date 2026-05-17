import React from "react";
import NestedDynamicMenu from "../../components/navigation/NestedDynamicMenu"; // Adjust the import path accordingly
export default {
    title: "Navigation/NestedDynamicMenu",
    component: NestedDynamicMenu,
    argTypes: {
    // Define your argTypes here if needed
    },
};

const Template = (args) => <div
> <NestedDynamicMenu {...args} /></div>;

const baseMenu = [{ "id": 8, "parent": 7, "droppable": false, "text": "Who is God?", "pageId": 165, "pageName": "Found-God" }, { "id": 7, "parent": 0, "droppable": true, "text": "Basics" }, { "id": 9, "parent": 7, "droppable": false, "text": "Who is Jesus Christ?", "pageId": 166, "pageName": "Found-Jesus" }, { "id": 10, "parent": 7, "droppable": false, "text": "Who is the Holy Ghost?", "pageId": 167, "pageName": "Found-Holy Ghost" }, { "id": 13, "parent": 7, "droppable": false, "text": "What is the Gospel of Jesus Christ?", "pageId": 170, "pageName": "Found-Gospel" }, { "id": 14, "parent": 7, "droppable": false, "text": "What is the Plan of Salvation?", "pageId": 173, "pageName": "Found-Plan Of Salvation" }, { "id": 19, "parent": 7, "droppable": false, "text": "What is Repentance?", "pageId": 174, "pageName": "Found-Repentance" }, { "id": 16, "parent": 7, "droppable": false, "text": "Baptism", "pageId": 172, "pageName": "Found-Baptism" }, { "id": 25, "parent": 7, "droppable": false, "text": "What is Grace?", "pageId": 171, "pageName": "Found-Grace" }, { "id": 17, "parent": 7, "droppable": false, "text": "The Sacrament/Communion", "pageId": 175, "pageName": "Found-Sacrament" }, { "id": 18, "parent": 7, "droppable": false, "text": "What is Scripture?", "pageId": 176, "pageName": "Found-Scripture" }, { "id": 21, "parent": 7, "droppable": false, "text": "What is Priesthood?", "pageId": 177, "pageName": "Found-Priesthood" }, { "id": 11, "parent": 7, "droppable": false, "text": "Where did I come from?", "pageId": 168, "pageName": "Found-PreExistence" }, { "id": 12, "parent": 7, "droppable": false, "text": "Why are we on the earth?", "pageId": 169, "pageName": "Found-Why Here" }, { "id": 23, "parent": 7, "droppable": false, "text": "What is a Spirit" }, { "id": 24, "parent": 7, "droppable": false, "text": "What happens when we die?" }, { "id": 20, "parent": 7, "droppable": false, "text": "What is a Prophet?", "pageId": 178, "pageName": "Found-Prophet" }, { "id": 15, "parent": 7, "droppable": false, "text": "Why do we have commandments?" }, { "id": 22, "parent": 7, "droppable": false, "text": "Why is an Organized Religion Important?" }, { "id": 1, "parent": 0, "droppable": true, "text": "A" }, { "id": 6, "parent": 1, "droppable": false, "text": "Apostasy", "pageId": 164, "pageName": "Apostascy" }, { "id": 3, "parent": 0, "droppable": true, "text": "B" }, { "id": 4, "parent": 1, "droppable": false, "text": "Aaron", "pageId": 257, "pageName": "Aaron" }, { "id": 5, "parent": 3, "droppable": false, "text": "Baal", "pageId": 162, "pageName": "Home" }, { "id": 39, "parent": 0, "droppable": true, "text": "C" }, { "id": 40, "parent": 0, "droppable": true, "text": "D" }, { "id": 41, "parent": 0, "droppable": true, "text": "E" }, { "id": 42, "parent": 0, "droppable": true, "text": "F" }, { "id": 31, "parent": 0, "droppable": true, "text": "G" }, { "id": 26, "parent": 0, "droppable": true, "text": "H" }, { "id": 37, "parent": 26, "droppable": false, "text": "Holy Ghost" }, { "id": 27, "parent": 26, "droppable": false, "text": "Horses in the Book of Mormon", "pageId": 179, "pageName": "BofM-Horses" }, { "id": 43, "parent": 0, "droppable": true, "text": "I" }, { "id": 44, "parent": 0, "droppable": true, "text": "J" }, { "id": 30, "parent": 28, "droppable": false, "text": "Prophets", "pageId": 178, "pageName": "Found-Prophet" }, { "id": 29, "parent": 28, "droppable": false, "text": "Prophets - The Test of a Prophet", "pageId": 180, "pageName": "Prophets-Test" }, { "id": 36, "parent": 31, "droppable": false, "text": "Grace", "pageId": 171, "pageName": "Found-Grace" }, { "id": 35, "parent": 31, "droppable": false, "text": "Gospel", "pageId": 170, "pageName": "Found-Gospel" }, { "id": 34, "parent": 31, "droppable": false, "text": "God - The Holy Ghost", "pageId": 167, "pageName": "Found-Holy-Ghost" }, { "id": 33, "parent": 31, "droppable": false, "text": "God - The Father", "pageId": 165, "pageName": "Found-God" }, { "id": 32, "parent": 31, "droppable": false, "text": "God - Jesus Christ", "pageId": 166, "pageName": "Found-Jesus" }, { "id": 45, "parent": 0, "droppable": true, "text": "K" }, { "id": 46, "parent": 0, "droppable": true, "text": "L" }, { "id": 47, "parent": 0, "droppable": true, "text": "M" }, { "id": 48, "parent": 0, "droppable": true, "text": "N" }, { "id": 49, "parent": 0, "droppable": true, "text": "O" }, { "id": 28, "parent": 0, "droppable": true, "text": "P" }, { "id": 57, "parent": 0, "droppable": true, "text": "Q" }, { "id": 58, "parent": 0, "droppable": true, "text": "R" }, { "id": 59, "parent": 58, "droppable": false, "text": "Repentance", "pageId": 174, "pageName": "Found-Repentance" }, { "id": 38, "parent": 0, "droppable": true, "text": "S" }, { "id": 60, "parent": 38, "droppable": false, "text": "Scripture", "pageId": 176, "pageName": "Found-Scripture" }, { "id": 50, "parent": 0, "droppable": true, "text": "T" }, { "id": 51, "parent": 0, "droppable": true, "text": "U" }, { "id": 52, "parent": 0, "droppable": true, "text": "V" }, { "id": 53, "parent": 0, "droppable": true, "text": "W" }, { "id": 54, "parent": 0, "droppable": true, "text": "X" }, { "id": 55, "parent": 0, "droppable": true, "text": "Y" }, { "id": 56, "parent": 0, "droppable": true, "text": "Z" }];

const transformData = (data) => {
    const map = {};
    const result = [];

    if (Array.isArray(data)) {
        // Create a map of all items
        data.forEach(item => {
            map[item.id] = { ...item, menuItems: [] };
        });

        // Build the tree structure
        data.forEach(item => {
            if (item.parent === 0) {
                result.push(map[item.id]);
            } else {
                if (map[item.parent]) {
                    map[item.parent].menuItems.push(map[item.id]);
                } else {
                    // Handle the case where the parent is not found
                    result.push(map[item.id]);
                }
            }
        });
    }

    return result;
};

const parentMenu = transformData(baseMenu);

export const Default = Template.bind({});
Default.args = {
    menuItems: parentMenu,
    // Provide initial arguments for your NestedDynamicMenu component if needed
};

Default.storyName = "Nested Dynamic Menu";
Default.parameters = {
    docs: {
        storyDescription: `
      The NestedDynamicMenu component allows you to create a nested menu structure where menus fill from left to right. Each menu can have its own sub-menu items.

      **Features**:
      - Nested menu structure
      - Customizable menu items

      **Usage**:
      Simply incorporate the NestedDynamicMenu component into your app and provide it with the necessary props for customization.
    `,
    },
};

// src/stories/TreeView.stories.jsx
import React from "react";
import TreeView, { MENU_CONTEXT_TYPE } from "../../components/treeview/TreeView";
import { action } from "@storybook/addon-actions";
import { node } from "prop-types";
export default {
    title: "Components/TreeView",
    component: TreeView,
};

const validateNodeName = (nodename) => {
    if (!nodename.trim()) {
        alert("name cannot be empty");
        return false;
    }
    return true;
};

const dynamicMenuSearchItems = [
    {
        type: "Custom Event", label: "Custom Event", onClick: async (nodename, menuItem) => {
            // Simulate API call with setTimeout
            const searchResult = await new Promise((resolve) => {
                setTimeout(() => {
                    resolve(`Custom Event for ${nodename}`);
                    return [{ search: true, reusult: false }];
                }, 2000); // Simulate 2 seconds delay
            });
            return true;
        }
    },
    {
        type: MENU_CONTEXT_TYPE.SEARCH_NODE, label: "Search Article", onClick: async (nodename, menuItem) => {
        // Simulate API call with setTimeout
            const searchResult = await new Promise((resolve) => {
                setTimeout(() => {
                    resolve(`Search result for ${nodename}`);
                    return [{ search: true, reusult: false }];
                }, 2000); // Simulate 2 seconds delay
            });
            return true;
        }
    },
    { type: MENU_CONTEXT_TYPE.SEARCH_PARENT, label: "Search Page", onClick: (nodename, menuItem) => { action("Search Container:" + nodename); } },];

const dynamicMenuItems = [
    {
        type: MENU_CONTEXT_TYPE.RENAME_NODE, label: "Rename Menu Item", onClick: (nodename, menuItem) => {

            return validateNodeName(nodename);
        }
    },
    {
        type: MENU_CONTEXT_TYPE.ADD_NODE, label: "Add Page", onClick: (nodename, menuItem) => {
            action("Add Page:" + nodename);
            return validateNodeName(nodename);
        }
    },
    {
        type: MENU_CONTEXT_TYPE.ADD_PARENT_NODE, label: "Add Container", onClick: (nodename,menuItem) => {
            action("Add Container:" + nodename);
            return validateNodeName(nodename);
        }
    },
    {
        type: MENU_CONTEXT_TYPE.DELETE, label: "Delete", onClick: (nodename, menuItem) => {
            action("Delete Node:" + nodename.text);
            return true;
        }
    }
];

const initialData = [
    { id: 1, dbId: 1, pageId: 0, articleID: 0, parent: 0, droppable: false, text: "Article 7" },
    { id: 2, dbId: 2, pageId: 0, articleID: 0, parent: 0, droppable: true, text: "Page 1" },
    { id: 3, dbId: 3, pageId: 0, articleID: 0, parent: 2, droppable: false, text: "Article 2-1" },
    { id: 4, dbId: 4, pageId: 0, articleID: 0, parent: 2, droppable: false, text: "Article 1-2-1" },
    { id: 5, dbId: 5, pageId: 0, articleID: 0, parent: 0, droppable: true, text: "Page 3" },
    { id: 6, dbId: 6, pageId: 0, articleID: 0, parent: 5, droppable: false, text: "Article 3-1" },
];

const handleNodeClicked = async (node) => {
    action("onTreeChanged");
    const searchResult = await new Promise((resolve) => {
        setTimeout(() => {
            resolve([{ search: true, reusult: false }]);
        }, 1000); // Simulate 2 seconds delay
    });
    return searchResult;
};

const handleTreeChanged = async (node) => {
    action("onTreeChanged");
    const searchResult = await new Promise((resolve) => {
        setTimeout(() => {
            resolve([{ search: true, reusult: false }]);
        }, 50); // Simulate 2 seconds delay
    });
    return searchResult;
};

const handleDelete = async (node) => {
    action("onNodeDeleted");
    const searchResult = await new Promise((resolve) => {
        setTimeout(() => {
            resolve(true);
        }, 50); // Simulate 2 seconds delay
    });
    return searchResult;
};

var dbidMax = 20;
const handleCreateNode = async (node) => {
    action("onCreateNode");
    const searchResult = await new Promise((resolve) => {
        setTimeout(() => {
            node.dbId = dbidMax;
            dbidMax++;
            resolve(node);
        }, 50); // Simulate 2 seconds delay
    });
    return searchResult;
};

const handleSearchNode = async (node) => {
    action("onSearch");
    const searchResult = await new Promise((resolve) => {
        setTimeout(() => {
            node.dbId = dbidMax;
            dbidMax++;
            //return value to show tree that node is found and context menu can be closed
            resolve(node);
        }, 2000); // Simulate 2 seconds delay
    });
    return searchResult;
};

const handleCustomEvent = async (nodeData,menuItem) => {
    action("onSearch");
    const searchResult = await new Promise((resolve) => {
        setTimeout(() => {
            node.dbId = dbidMax;
            dbidMax++;
            //return value to show tree that node is found and context menu can be closed
            resolve(node);
        }, 2000); // Simulate 2 seconds delay
    });
    return searchResult;
};

const Template = (args) => <TreeView {...args} />;


export const Default = Template.bind({});
Default.args = {
    menuItems:dynamicMenuItems,
    initialData: initialData,
    onParentClick: handleNodeClicked,
    onDelete: handleDelete,
    onNodeClick: handleNodeClicked,
    onTreeChanged: handleTreeChanged,
    onSearchParent: action("onSearchParent"),
    onSearchNode: action("onSearchNode"),
    onCreateNode: handleCreateNode,
    onCreateParent: handleCreateNode
};

export const SearchProcessing = Template.bind({});
SearchProcessing.args = {
    menuItems: dynamicMenuSearchItems,
    initialData: initialData,
    onParentClick: handleNodeClicked,
    onDelete: handleDelete,
    onNodeClick: handleNodeClicked,
    onTreeChanged: handleTreeChanged,
    onSearchParent: handleSearchNode,
    onSearchNode: handleSearchNode,
    onCreateNode: handleCreateNode,
    onCreateParent: handleCreateNode,
    onCustomEvent:handleCustomEvent
};

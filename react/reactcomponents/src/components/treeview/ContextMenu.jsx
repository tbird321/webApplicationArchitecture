import React, { useEffect, useRef, useState } from "react";

function ContextMenu({ position, menuItems, onAddNode, onAddParent, onClose,onSearchParent,onSearchNode,onDelete,onRenameNode,onCustomEvent }) {
    const [nodeName, setNodeName] = useState("");
    const menuRef = useRef(null);

    useEffect(() => {
        const handleClickOutside = (event) => {
            if (menuRef.current && !menuRef.current.contains(event.target)) {
                onClose();
            }
        };

        document.addEventListener("mousedown", handleClickOutside);
        return () => {
            document.removeEventListener("mousedown", handleClickOutside);
        };
    }, [onClose]);

    const handleInputChange = (event) => {
        setNodeName(event.target.value);
    };

    const handleAddNodeClick = (menuItem) => {
        onAddNode(nodeName, menuItem);
        setNodeName(""); // Clear the input after adding the node
    };

    const handleAddParentClick = (menuItem) => {
        onAddParent(nodeName, menuItem);
        setNodeName(""); // Clear the input after adding the page
    };

    const handleSearchParentClick = (menuItem) => {
        if (onSearchParent, menuItem) {
            onSearchParent(nodeName,menuItem);
        }
    };

    const handleSearchNodeClick = (menuItem) => {
        if (onSearchNode, menuItem) {
            onSearchNode(nodeName,menuItem);
        }
    };

    const handleDeleteClick = (menuItem) => {
        if (onDelete, menuItem) {
            onDelete(nodeName,menuItem);
        }
    };

    const handleRenameNodeClick = (menuItem) => {
        if (onRenameNode, menuItem) {
            onRenameNode(nodeName,menuItem);
        }
        setNodeName(""); // Clear the input after adding the page
    };
    const handleContextMenuItemClick = (menuItem) => {
        switch (menuItem.type) {
        case "ADD_NODE":
            handleAddNodeClick(menuItem);
            break;
        case "RENAME_NODE":
            handleRenameNodeClick(menuItem);
            break;
        case "ADD_PARENT_NODE":
            handleAddParentClick(menuItem);
            break;
        case "SEARCH_PARENT":
            handleSearchParentClick(menuItem);
            break;
        case "SEARCH_NODE":
            handleSearchNodeClick(menuItem);
            break;
        case "DELETE":
            handleDeleteClick(menuItem);
            break;
        default:
            if (onCustomEvent) {
                onCustomEvent(nodeName,menuItem);
            }
            break;
        }

    };
    if (!position) return null;

    return (
        <div
            ref={menuRef}
            style={{
                position: "absolute",
                top: position.y,
                left: position.x,
                background: "white",
                boxShadow: "0px 0px 5px rgba(0,0,0,0.3)",
                zIndex: 1000,
                padding: "5px",
            }}
        >
            <input
                type="text"
                value={nodeName}
                onChange={handleInputChange}
                placeholder="Enter name"
                style={{ marginBottom: "5px", width: "100%" }}
            />
            {/* Render dynamic menu items */}
            {menuItems.map((menuItem, index) => (
                <button key={index} onClick={() => {
                    handleContextMenuItemClick(menuItem);
                }} style={{ width: "100%" }}>
                    {menuItem.label}
                </button>
            ))}
        </div>
    );
}

export default ContextMenu;

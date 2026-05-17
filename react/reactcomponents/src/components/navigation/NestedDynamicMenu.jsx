// NestedDynamicMenu.jsx
import React, { useEffect } from "react";
import DynamicMenu from "./DynamicMenu";
import "./NestedDynamicMenu.css"; // Import the CSS file for styling

function NestedDynamicMenu({ menuItems, onClick, keepOpen, showTitles = false }) {
    useEffect(() => {
        if (typeof window !== "undefined" && window.__APP_LOGGING_ENABLED__) {
            // eslint-disable-next-line no-console
            console.log("NestedDynamicMenu items:", menuItems);
        }
    }, [menuItems]);
    return (
        <div className="nested-dynamic-menu">
            {menuItems?.map((menu, index) => (
                <DynamicMenu key={index} parentMenuItem={menu} onClick={onClick} keepOpen={keepOpen} showTitles={showTitles} />
            ))}
        </div>
    );
}

export default NestedDynamicMenu;

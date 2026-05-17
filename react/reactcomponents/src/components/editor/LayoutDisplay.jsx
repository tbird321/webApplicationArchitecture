import React from "react";
import "./LayoutDisplay.css";
import availableLayouts from "../styling/AvailableLayouts";

const LayoutDisplay = ({ currentLayout }) => {

    const diagram = availableLayouts.get(currentLayout);
    if (!diagram) {
        return <div>Layout not found</div>;
    }

    return (
        <>
            <img
                className="imageWidth"
                src={diagram}
                alt={`Layout diagram for ${currentLayout.name}`} />
        </>
    );
};

export default LayoutDisplay;

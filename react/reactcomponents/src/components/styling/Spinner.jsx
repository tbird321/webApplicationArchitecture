import React from "react";
import "./Spinner.css";

function Spinner({ showSpinner }) {
    return (
        <>
            {showSpinner === true && <div className="spinner-container">
                <img src={`${process.env.PUBLIC_URL}/Gear.gif`} alt="Loading..." />
            </div>}
        </>
    );
}

export default Spinner;

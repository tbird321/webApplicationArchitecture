import React from 'react';
const HtmlContentRenderer = ({ htmlContent }) => {

    // Ensure the HTML content is sanitized and safe before rendering
    // It's important to sanitize htmlContent to prevent XSS attacks (e.g., using a library like DOMPurify)
    return (
        <div>
            {htmlContent && <div dangerouslySetInnerHTML={{ __html: htmlContent }} />}
        </div>
    );
};

export default HtmlContentRenderer;

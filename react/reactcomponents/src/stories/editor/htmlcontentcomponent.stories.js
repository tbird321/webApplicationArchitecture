// File: HtmlContentRenderer.stories.js

import React from "react";
import HtmlContentRenderer from "../../components/editor/HtmlContentRenderer"; // Adjust the import path as necessary

export default {
    title: "editor/HtmlContentRenderer",
    component: HtmlContentRenderer,
    argTypes: {
        htmlContent: { control: "text" } // Define a control for the HTML content
    },
};

// Template that configures the component with the args (htmlContent in this case)
const Template = ({ ...args }) => {
    return <HtmlContentRenderer {...args} />;
};

export const DynamicHtmlContent = Template.bind({});
DynamicHtmlContent.args = {
    htmlContent: "<p>This is your initial HTML content. You can edit it!</p>", // Initial content
};

/*
const fetchHtmlContent = async () => {
    // Replace 'path/to/your/file.html' with the actual path to your file in storage
    const file = await Storage.get('path/to/your/file.html', { download: true });

    // Convert the blob to text
    if (file.Body) {
      return new Response(file.Body).text();
    }

    throw new Error('Failed to fetch file');
  };
*/

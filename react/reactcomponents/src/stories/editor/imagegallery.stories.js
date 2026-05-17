import React from "react";
import ImageGallery from "../../components/editor/ImageGallery";
import imageList from "../constants/imageList";

// This default export determines where your story goes in the story list
export default {
    title: "editor/ImageGallery",
    component: ImageGallery,
};

const Template = (args) =>
    <ImageGallery {...args} />;

export const Default = Template.bind({});
Default.args = {
    images: imageList,
    width: "400px",
    height:"400px",
    columns: 3,
    rowHeight: 100,
    rowWidth: 100,
    gap: 5,
    onSelectImage: (image) => console.log("Selected image:", image),
};

// You can add more variations of the component
export const Empty = Template.bind({});
Empty.args = {
    images: [],
    onSelectImage: (image) => console.log("Selected image:", image),
    columns:10,
    gap :2
};

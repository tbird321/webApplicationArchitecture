/// <reference path="stylingconstants.js" />
import stylingConstants from "./StylingConstants";

const availableCSSAttributes = [
    // Color and Background
    {name:"color", label: "Color", type:"color", },
    {name:"background-color", label: "Background Color", type:"color", },
    {name:"background-position", label: "Background Position", type: "dropdown", dropType: stylingConstants.position , },
    {name:"background-repeat", label: "Background Repeat", type: "dropdown", dropType:stylingConstants.repeat, },
    {name:"background-size", label: "Background Size", type:"size", },

    // Font and Text
    {name:"font-family", label: "Font", type:"font", },
    {name:"font-size", label: "Font Size", type:"size", },
    {name:"font-weight", label: "Font Weight", type:"dropdown",dropType: stylingConstants.fontweight, },
    {name:"text-align", label: "Text Align", type: "dropdown", dropType: stylingConstants.position, },
    {name:"text-decoration", label: "Text Decoration", type: "dropdown", dropType: stylingConstants.decoration, },
    {name:"line-height", label: "Line Height", type:"size", },

    // Box Model
    {name:"margin-top", label: "Margin Top", type:"size", },
    {name:"margin-right", label: "Margin Right", type:"size", },
    {name:"margin-bottom", label: "Margin Bottom", type:"size", },
    {name:"margin-left", label: "Margin Left", type:"size", },
    {name:"padding-top", label: "Padding Top", type:"size", },
    {name:"padding-right", label: "Padding Right", type:"size", },
    {name:"padding-bottom", label: "Padding Bottom", type:"size", },
    {name:"padding-left", label: "Padding Left", type:"size", },
    {name:"border-radius", label: "Border Radius", type:"size", },
    {name:"border-width", label: "Border Width", type:"size", },
    {name:"border-color", label: "Border Color", type:"size", },
    {name:"border-style", label: "Border Style", type: "dropdown", dropType:stylingConstants.borderStyle, },
    {name:"width", label: "Width", type:"size", },
    {name:"height", label: "Height", type:"size", },

    // Layout and Positioning

    // "display", new type    still needs to be done
    {name:"position", label: "Block Position", type: "dropdown", dropType:stylingConstants.blockPosition, },
    {name:"top", label: "Top", type:"size", },
    {name:"right", label: "Right", type:"size", },
    {name:"bottom", label: "Bottom", type:"size", },
    {name:"left", label: "Left", type:"size", },
    {name:"overflow", label: "Overflow", type: "dropdown", dropType:stylingConstants.overflow, },
    {name:"overflow-x", label: "Overflow X", type: "dropdown", dropType:stylingConstants.overflow, },
    {name:"overflow-y", label: "Overflow Y", type: "dropdown", dropType:stylingConstants.overflow, },
    {name:"float", label: "Float", type: "dropdown", dropType: stylingConstants.float, },
    {name:"vertical-align", label: "Vertical Align", type: "dropdown", dropType:stylingConstants.verticalAlign, },
    // "z-index", new type integer    still needs to be done


    // Others
    { name: "display", label: "Display", type: "dropdown", dropType: stylingConstants.display },
    { name: "justify-content", label: "Justify Content", type: "dropdown", dropType: stylingConstants.justifyContent },
    { name: "align-items", label: "Align Items", type: "dropdown", dropType: stylingConstants.alignItems },
    { name: "flex-direction", label: "Flex Direction", type: "dropdown", dropType: stylingConstants.flexDirection },
    {name:"visibility", label: "Visibility", type:"visibility", },
    {name:"list-style-type", label: "List Style Type", type:"listStyleType", },

    //  "opacity", new type percentage still needs done
];
export default availableCSSAttributes;

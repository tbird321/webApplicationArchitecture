import React, { useState, useEffect } from "react";
import { Editor } from "@tinymce/tinymce-react";
import { availableFonts } from "../styling";
import "./HtmlEditor.css";
import ImageGallery from "./ImageGallery"; // Import the ImageGallery component
import ModalDialog from "../ModalDialog"; // Adjust the import path accordingly


const HtmlEditor = ({ initialHtml, onSave, filePickerCallback, imagesUploadHandler, images, onChange, cssfilePath, styleClasses }) => {

    const [editor, setEditor] = useState(null);
    const [showGallery, setShowGallery] = useState(false);
    const [cssFileName, setcssFileName] = useState(cssfilePath);
    const [selectedImage, setSelectedImage] = useState(null);
    const [blockFormats, setBlockFormats] = useState("Paragraph=p; Heading 1=h1; Heading 2=h2; Heading 3=h3;Div=div");
    const [formats, setFormats] = useState({
        surround: { block: "div", classes: "newContainer" }
    });
    const computedStyleFormats = (Array.isArray(styleClasses) ? styleClasses : [])
        .filter(Boolean)
        .map((cls) => ({ title: `.${cls}`, inline: "span", classes: cls }));

    const handleSelectImage = (image) => {
        setSelectedImage(image);
        setShowGallery(false);
        // Logic to insert image URL in the editor
        if (editor) {
            editor.insertContent(`<img src="${image.url}" alt="${image.alt}" width="100px" />`);
        }
    };

    const handleEditorChange = (a,editor) => {
        if(onChange){
            onChange(editor.getContent());
        }
    };

    const handleEditorInit = (editor) => {
        setEditor(editor);
    };

    useEffect(() => {
        setcssFileName(cssfilePath);
    }, [cssfilePath]);

    useEffect(() => {
        if (editor) {
            editor.focus();
        }
    }, [editor]);

    const handleSave = (editorContent) => {
        if (onSave) {
            onSave(editorContent);
        }
    };

    const onOpenImageBrowser =(callback,value,meta) =>{
        setShowGallery(true);
        if (filePickerCallback)
        {
            filePickerCallback(callback,value,meta);
        }
    };

    const handleImageUpload = (blobInfo, success, failure) => {
        return new Promise((resolve, reject) => {
            if(imagesUploadHandler){
                const newLoc=imagesUploadHandler(blobInfo, success, failure);
                resolve(newLoc);
            }else
            {
                reject("false");
            }
        });
    };

    function generateFontFormats(fonts) {
        var fontmap= fonts?.map(font => `${font}=${font.toLowerCase().replace(/ /g, " ")};`).join(" ");
        return fontmap;
    }

    const tinyMCEFontFormats = generateFontFormats(availableFonts);

    const wrapSelectedText = (editor) => {
        const selectedText = editor.selection.getContent();
        const wrappedContent = `<div class="newContainer">${selectedText}</div>`;

        if (selectedText.length > 0) {
            editor.selection.setContent(wrappedContent);
        }
    };


    return (
        <div className="html-editor-container">
            <div className="content">
                <div className="resizable-container">
                    <Editor
                        key={cssFileName }//cause remount on styling theme change
                        apiKey='8iei1nvi4yql5yhd8i3bscmul4xd087raorzhearppheyq39'
                        initialValue={initialHtml}
                        images_reuse_filename={true}
                        content_css={cssfilePath}
                        onInit={(e, editor) => handleEditorInit(editor)}
                        images_upload_url='postAcceptor.php'
                        automatic_uploads={false}
                        init={{
                            menubar: true,
                            fontsize_formats: "8pt 10pt 12pt 14pt 18pt 24pt 36pt",
                            font_formats: tinyMCEFontFormats,
                            block_formats: blockFormats,
                            formats: formats,
                            style_formats: computedStyleFormats,
                            autoresize_min_height: 200,
                            autoresize_max_height: 600,
                            "templates": [
                                {
                                    "title": "Wrapping div",
                                    "description": "add a div with content",
                                    "content": "<div>This is some sample code to add</div>"
                                }
                            ],
                            plugins: [
                                "advlist", "anchor", "autolink", "charmap", "code", "fullscreen","autoresize",
                                "help", "image", "insertdatetime", "link", "lists", "media","template",
                                "preview", "searchreplace", "table", "visualblocks","quickbars"
                            ],
                            quickbars_selection_toolbar:"bold italic styles quicklink blockquote",
                            menu: {
                                file: { title: "File", items: "save restoredraft | preview | print " }
                            },
                            toolbar: "wrapText applyClass removeClasses|undo redo bold styles template italic underline strikethrough alignleft aligncenter alignright alignjustify bullist numlist outdent indent link image media table forecolor backcolor removeformat hr anchor wordcount code",
                            quickbars_insert_toolbar: "quickimage quicktable | hr pagebreak",
                            quickbars_image_toolbar: "alignleft aligncenter alignright | rotateleft rotateright | imageoptions",
                            setup: (editor) => {editor.ui.registry.addContextToolbar("imageselection", {
                                predicate: function(node) {
                                    return node.nodeName === "P";
                                },
                                items: "quicklink",
                                position: "node"
                            });
                            editor.ui.registry.addButton("wrapText", {
                                text: "Wrap Text",
                                onAction: () => {
                                    wrapSelectedText(editor);
                                }
                            });
                            editor.ui.registry.addButton("applyClass", {
                                text: "Apply Class",
                                onAction: () => {
                                    const cls = window.prompt("Enter CSS class name(s) to apply (space-separated):");
                                    if (!cls) return;
                                    const selection = editor.selection.getContent({ format: "html" });
                                    if (selection && selection.length > 0) {
                                        editor.selection.setContent(`<span class="${cls}">${selection}</span>`);
                                    } else {
                                        // No selection: toggle class on current node
                                        const node = editor.selection.getNode();
                                        if (node) {
                                            const existing = (node.getAttribute && node.getAttribute("class")) || "";
                                            const merged = (existing ? (existing + " ") : "") + cls;
                                            editor.dom.setAttrib(node, "class", merged.trim());
                                        }
                                    }
                                }
                            });
                            editor.ui.registry.addButton("removeClasses", {
                                text: "Remove Classes",
                                onAction: () => {
                                    const node = editor.selection.getNode();
                                    if (node) {
                                        editor.dom.setAttrib(node, "class", null);
                                    }
                                }
                            });
                            editor.ui.registry.addMenuItem("save", {
                                text: "Save",
                                onAction: () => {
                                    handleSave(editor.getContent());
                                }
                            });
                            editor.ui.registry.addMenuButton("file", {
                                text: "File",
                                fetch: (callback) => {
                                    var items = [
                                        { type: "menuitem", text: "Save", onAction: () => handleSave(editor.getContent()) }
                                        // Add more file menu items here if needed
                                    ];
                                    callback(items);
                                }
                            });
                            },
                            image_advtab: true,
                            images_upload_handler: handleImageUpload,
                            file_picker_callback: function (callback, value, meta) {
                                if (meta.filetype === "image") {
                                    // Open your custom image browser
                                    onOpenImageBrowser(callback,value,meta);
                                }
                            },
                            resize: false,
                            content_css: cssFileName
                        }}
                        onChange={handleEditorChange}
                    />
                </div>
            </div>
            <div className="html-editor-container">
                <ModalDialog open={showGallery} onClose={()=>{setShowGallery(false);}}>
                    <ImageGallery images={images} onSelectImage={handleSelectImage}
                        width= '400px'
                        height='400px'
                        columns= '3'
                        rowHeight= '100'
                        rowWidth= '100'
                        gap= '5'  />
                </ModalDialog>
            </div>
        </div>
    );
};

export default HtmlEditor;

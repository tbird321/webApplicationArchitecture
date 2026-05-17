import React, { useState } from "react";
import Button from "@mui/material/Button";
import TextField from "@mui/material/TextField";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import Stack from "@mui/material/Stack";
import HtmlEditor from "./HtmlEditor";

const TemplateModal = ({ editMode, currentTemplate, onTemplateSave, onIsDirty }) => {
    const [title, setTitle] = useState(editMode ? currentTemplate.title : "");
    const [description, setDescription] = useState(editMode ? currentTemplate.description : "");
    const [content, setContent] = useState(editMode ? currentTemplate.content : "");

    const generateUniqueId = () => Date.now();

    const handleSaveButton = () => {
        const templateToSave = {
            ...currentTemplate,
            id: editMode ? currentTemplate.id : generateUniqueId(),
            title,
            description,
            content
        };
        onTemplateSave(templateToSave);
        onIsDirty(false);
    };

    const handleHtmlChange = (newContent) => {
        setContent(newContent);
        onIsDirty(true);
    };

    return (
        <Box sx={{ margin: theme => theme.spacing(3) }}>
            <Typography id="modal-modal-title" variant="h6" component="h2">
                <Stack spacing={2} sx={{ width: 400, marginTop: theme => theme.spacing(2) }}>
                    <TextField
                        type="text"
                        label="Title"
                        variant="standard"
                        value={title}
                        onChange={e => setTitle(e.target.value)}
                    />

                    <TextField
                        type="text"
                        label="Description"
                        variant="standard"
                        value={description}
                        onChange={e => setDescription(e.target.value)}
                    />
                </Stack>
            </Typography>
            <HtmlEditor
                initialHtml={content}
                onsave={handleSaveButton}
                onChange={handleHtmlChange}
            />
            <Button onClick={handleSaveButton}>Save Template</Button>
        </Box>
    );
};

export default TemplateModal;

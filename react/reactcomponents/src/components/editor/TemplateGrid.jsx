import React, { useState, useEffect } from "react";
import Button from "@mui/material/Button";
import Box from "@mui/material/Box";
import { DataGrid } from "@mui/x-data-grid";
import Grid from "@mui/material/Unstable_Grid2";
import ModalDialog from "../ModalDialog";
import TemplateModal from "./TemplateModal";
import "./TemplateGrid.css";

const DEFAULT_TEMPLATE = {
    id: null,
    title: "",
    description: "",
    content: ""
};

const TemplateGrid = ({ columns, templates, pageSize, onSaveTemplates }) => {
    const [templateModalOpen, setTemplateModalOpen] = useState(false);
    const [isDirty, setIsDirty] = useState(false);
    const [templatesLoaded, setTemplatesLoaded] = useState([]);
    const [editMode, setEditMode] = useState(false);
    const [currentTemplate, setCurrentTemplate] = useState(DEFAULT_TEMPLATE);

    useEffect(() => {
        setTemplatesLoaded(templates || []);
    }, [templates]);

    const handleRowClick = (selectionModel) => {
        setEditMode(true);
        setCurrentTemplate(selectionModel.row);
        setTemplateModalOpen(true);
    };

    const handleClose = () => {
        setTemplateModalOpen(false);
        setEditMode(false);
    };

    const handleSaveTemplates = (templateData) => {
        onSaveTemplates?.(templateData);
        setIsDirty(false);
    };

    const handleTemplateSave = (newTemplate) => {
        const index = templatesLoaded.findIndex(template => template.id === newTemplate.id);
        const updatedTemplates = [...templatesLoaded];
        if (index !== -1) {
            updatedTemplates[index] = newTemplate;
        } else {
            updatedTemplates.push(newTemplate);
        }
        setTemplatesLoaded(updatedTemplates);
        handleClose();
    };

    const handleDelete = (id) => {
        setTemplatesLoaded(templatesLoaded.filter(template => template.id !== id));
        setCurrentTemplate(prevTemplate => prevTemplate.id === id ? DEFAULT_TEMPLATE : prevTemplate);
    };

    const handleIsDirty = (boolean) => {
        setIsDirty(boolean);
    };

    const renderDeleteCell = (params) => (
        <Button
            color="error"
            onClick={(event) => {
                event.stopPropagation();
                handleDelete(params.row.id);
            }}
        >
            Delete
        </Button>
    );

    const generateColumns = () => {
        return [
            ...columns?.map(({ field, Header, Type }) => ({
                field,
                headerName: Header,
                flex: 1.5,
                type: Type,
                renderCell: field === "type" ? (params) => params.row.type?.label || "" : undefined
            })),
            {
                field: "delete",
                headerName: "",
                flex: 1,
                renderCell: renderDeleteCell
            }
        ];
    };
    const dataGridColumns = generateColumns(columns);

    return (
        <div>
            <Grid >
                <Box >
                    <div>Templates</div>
                    <DataGrid
                        sx={{ "--unstable_DataGrid-radius": "0" }}
                        rows={templatesLoaded}
                        columns={dataGridColumns}
                        rowHeight={54}
                        initialState={{}}
                        onRowClick={handleRowClick}
                        pageSizeOptions={pageSize??[]}
                    />
                    <Button onClick={() => setTemplateModalOpen(true)}>New Template</Button>
                    <Button onClick={() => handleSaveTemplates(templatesLoaded)}>Save Templates</Button>
                </Box>
                <ModalDialog
                    open={templateModalOpen}
                    childDirty={isDirty}
                    onClose={handleClose}
                    aria-labelledby="modal-modal-title"
                    aria-describedby="modal-modal-description"
                >
                    <TemplateModal
                        editMode={editMode}
                        currentTemplate={currentTemplate}
                        onTemplateSave={handleTemplateSave}
                        onIsDirty={handleIsDirty}
                    />
                </ModalDialog>
            </Grid>
        </div>
    );
};

export default TemplateGrid;

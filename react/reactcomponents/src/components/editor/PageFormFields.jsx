// FormFields.jsx
import React, { useState, useEffect, useRef } from "react";
import TextField from "@mui/material/TextField";
import Autocomplete from "@mui/material/Autocomplete";
import Grid from "@mui/material/Unstable_Grid2"; // Grid version 2
import Chip from "@mui/material/Chip";
import Box from "@mui/material/Box";

import ModalDialog from "../ModalDialog";
import List from "@mui/material/List";
import ListItem from "@mui/material/ListItem";
import ListItemText from "@mui/material/ListItemText";
import availableLayouts from "../styling/AvailableLayouts";
import LayoutDisplay from "./LayoutDisplay";

const PageFormFields = ({ currentPage, onPageChange, styleSheets, topicsOptions, keywordOptions }) => {

    const [layoutMap, setLayoutMap] = useState(availableLayouts);
    const [openKeywordDialog, setOpenKeywordDialog] = useState(false);
    const [openTopicDialog, setOpenTopicDialog] = useState(false);
    const [selectedLayout, setSelectedLayout] = useState(currentPage.layout);

    const handleOnPageChange = (field, value) =>
    {
        if (field === "layout") {
            setSelectedLayout(value);
        }
        onPageChange(field, value);
    };


    const renderTags = (setter) => (value, getTagProps) => {
        return (
            <>
                {value?.slice(0, 3)?.map((option, index) => (
                    <Chip {...getTagProps({ index })} label={option} key={index} />
                ))}
                {value?.length > 3 && (
                    <Chip label={`+${value?.length - 3} more`} onClick={() => {
                        setter(true);
                    }} />
                )}
            </>
        );
    };

    return (
        <div>
            <Grid container spacing={2} >
                <Grid item xs={9}>
                    <div>
                        <Grid container spacing={2} >
                            <Grid item xs={4}>
                                <TextField
                                    type="name"
                                    id="name standard-basic"
                                    label="Name"
                                    variant="standard"
                                    value={currentPage.name}
                                    onChange={(e) => handleOnPageChange("name", e.target.value)}
                                />
                            </Grid>
                            <Grid item xs={4}>
                                <TextField
                                    type="description"
                                    id="description standard-basic"
                                    label="Description"
                                    variant="standard"
                                    value={currentPage.description}
                                    onChange={(e) => handleOnPageChange("description", e.target.value)}
                                />
                            </Grid>
                            <Grid item xs={4}>
                                <Autocomplete
                                    id="tags-standard"
                                    options={styleSheets??[]}
                                    defaultValue={currentPage.style}
                                    onChange={(e, newValue) => handleOnPageChange("style", newValue)}
                                    renderInput={(params) => (
                                        <TextField
                                            {...params}
                                            type="text"
                                            id="style standard-basic"
                                            label="Styles"
                                            variant="standard"
                                        />
                                    )}
                                />
                            </Grid>
                            <Grid item xs={4}>
                                <Autocomplete
                                    multiple
                                    id="tags-standard"
                                    options={topicsOptions??[]}
                                    defaultValue={currentPage.topics}
                                    renderTags={(value, getTagProps) => renderTags(setOpenKeywordDialog)(value, getTagProps)}
                                    onChange={(e, newValue) => handleOnPageChange("topics", newValue)}
                                    renderInput={(params) => (
                                        <TextField
                                            {...params}
                                            type="text"
                                            id="Topics-standard-basic"
                                            label="Topics"
                                            variant="standard"
                                        />
                                    )}
                                />
                                <ModalDialog open={openKeywordDialog} aria-labelledby="modal-modal-title" aria-describedby="modal-modal-description" onClose={() => {
                                    setOpenKeywordDialog(false);
                                }}  >
                                    <List>
                                        {currentPage.topics?.map((topic, index) => (
                                            <ListItem key={index}>
                                                <ListItemText primary={topic} />
                                            </ListItem>
                                        ))}
                                    </List>
                                </ModalDialog>
                            </Grid>
                            <Grid item xs={4}>
                                <Autocomplete
                                    multiple
                                    id="tags-standard"
                                    options={keywordOptions??[]}
                                    defaultValue={currentPage.keywords}
                                    renderTags={(value, getTagProps) => renderTags(setOpenTopicDialog)(value, getTagProps)}
                                    onChange={(e, newValue) => handleOnPageChange("keywords", newValue)}
                                    renderInput={(params) => (
                                        <TextField
                                            {...params}
                                            type="text"
                                            id="keywords-standard-basic"
                                            label="Keywords"
                                            variant="standard"
                                        />
                                    )}
                                />
                                <ModalDialog open={openTopicDialog} aria-labelledby="modal-modal-title" aria-describedby="modal-modal-description" onClose={() => {
                                    setOpenTopicDialog(false);
                                }}  >
                                    <List>
                                        {currentPage.keywords?.map((topic, index) => (
                                            <ListItem key={index}>
                                                <ListItemText primary={topic} />
                                            </ListItem>
                                        ))}
                                    </List>
                                </ModalDialog>
                            </Grid>
                            <Grid item xs={4}>
                                <Autocomplete
                                    id="layout standard"
                                    options={Array.from(layoutMap.keys()) ?? []}
                                    defaultValue={currentPage.layout}
                                    onChange={(e, newValue) => {
                                        handleOnPageChange("layout", newValue);
                                    }}
                                    renderInput={(params) => (
                                        <TextField
                                            {...params}
                                            type="text"
                                            id="layout"
                                            label="Article Layout"
                                            variant="standard"
                                        />
                                    )}
                                />
                            </Grid>
                        </Grid>
                    </div>
                </Grid>
                <Grid item xs={3}>
                    <Box>
                        <LayoutDisplay
                            currentLayout={selectedLayout}
                        />
                    </Box>
                </Grid>
            </Grid>
        </div>
    );
};

export default PageFormFields;

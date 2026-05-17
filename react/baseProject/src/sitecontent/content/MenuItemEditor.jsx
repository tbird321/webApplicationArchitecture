// MenuItemEditor.jsx
import React, { useState, useEffect } from "react";
import TextField from "@mui/material/TextField";
import Button from "@mui/material/Button";
import MenuItem from "@mui/material/MenuItem";
import Box from "@mui/material/Box";

function MenuItemEditor({ item, onUpdate, onItemChanged, onAddBelow, onDeleteMenuItem, onCanceled, disabled, onOpenSearch, onSearchType }) {
    // State for the form inputs. Initialize with `item` prop.
    const [menuItemData, setMenuItemData] = useState(item || { id: "", text: "" });

    // Update state when `item` prop changes.
    useEffect(() => {
        setMenuItemData(item);
    }, [item]);
    const handleOpenSearchPage = () => {
        onSearchType('page')
        onOpenSearch();
    };
    const handleOpenSearchCollection = () => {
        onSearchType('collection')
        onOpenSearch();
    };
    // Handle input changes
    const handleInputChange = (event) => {
        const { name, value } = event.target;
        var updatedItem = {
            ...menuItemData,
            [name]: value || "",
        };
        setMenuItemData(updatedItem);
        if (onItemChanged) {
            onItemChanged(updatedItem);
        }
    };

    // Handle save button click
    const handleSave = () => {
        if (onUpdate) {
            onUpdate(menuItemData);
        }
    };

    const handleRemovePage = () => {
        var updatedItem = {
            ...menuItemData
        };
        updatedItem.pageId = null;
        updatedItem.pageName = '';
        setMenuItemData(updatedItem);
        if (onUpdate) {
            onUpdate(updatedItem);
        }
    };

    return (
        <Box
            component="form"
            sx={{
                "& .MuiTextField-root": { m: 1, width: "25ch" },
            }}
            noValidate
            autoComplete="off"
        >
            <MenuItem>
                <TextField
                    name="id"
                    label="Unique ID"
                    value={menuItemData.id == null ? "" : menuItemData.id}
                    onChange={handleInputChange}
                    fullWidth
                    disabled={disabled}
                />
            </MenuItem>
            <MenuItem>
                <TextField
                    name="text"
                    label="Text"
                    value={menuItemData.text == null ? "" : menuItemData.text}
                    onChange={handleInputChange}
                    fullWidth
                    disabled={disabled}
                />
            </MenuItem>
            <MenuItem>
                <TextField
                    name="itemTitle"
                    label="Hover Title"
                    placeholder="Shown on hover; describe the target or purpose"
                    value={menuItemData.itemTitle == null ? "" : menuItemData.itemTitle}
                    onChange={handleInputChange}
                    fullWidth
                    disabled={disabled}
                />
            </MenuItem>
            <MenuItem>
                {disabled && menuItemData.pageId != null && (
                    <TextField
                        name="pageName"
                        label="Page Name"
                        value={menuItemData.pageName}
                        onChange={handleInputChange}
                        fullWidth
                        disabled={disabled}
                    />
                )}
                {!disabled && !menuItemData.pageId != null && (
                    <div style={{ minHeight: '60px',paddingTop:'10px' }} >
                         <Button
                            onClick={handleOpenSearchPage}
                            color="primary"
                            variant="contained"
                        >
                            Add Page
                        </Button>
                        <div style={{ paddingLeft: '10px', display: 'inline' }}>
                            <Button
                                onClick={handleOpenSearchCollection}
                                color="primary"
                                variant="contained"
                            >
                                Add Collection
                            </Button>
                        </div>
                        <div style={{paddingLeft:'10px',display:'inline'} }>
                        <Button
                            onClick={handleRemovePage}
                            color="primary"
                            variant="contained"
                        >
                            Remove Page
                            </Button>
                        </div>
                    </div>
                )}
            </MenuItem>
            {!disabled &&
                <MenuItem>
                    <div style={{ display: "flex", gap: "8px" }}>
                        <Button variant="contained" onClick={handleSave}>Update</Button>
                        <Button
                            onClick={onAddBelow}
                            color="primary"
                            variant="contained"
                        >
                            Add Below
                        </Button>
                        <Button
                            onClick={onDeleteMenuItem}
                            color="primary"
                            variant="contained"
                        >
                            Delete
                        </Button>
                        <Button
                            onClick={onCanceled}
                            color="primary"
                            variant="contained"
                        >
                            Cancel
                        </Button>
                    </div>

                </MenuItem>}
        </Box>
    );
}

export default MenuItemEditor;

// MenuItemEditor.jsx
import React, { useState, useEffect } from "react";
import TextField from "@mui/material/TextField";
import Button from "@mui/material/Button";
import MenuItem from "@mui/material/MenuItem";
import Box from "@mui/material/Box";

function MenuItemEditor({ item, onUpdate, onItemChanged, onAddBelow, onDeleteMenuItem, onCanceled, disabled, onOpenSearch }) {
    // State for the form inputs. Initialize with `item` prop.
    const [menuItemData, setMenuItemData] = useState(item || { id: "", text: "" });

    // Update state when `item` prop changes.
    useEffect(() => {
        setMenuItemData(item);
    }, [item]);
    const handleOpenSearch = () => {
        onOpenSearch();
    };
    // Handle input changes
    const handleInputChange = (event) => {
        const { name, value } = event.target;
        var updatedItem={
            ...menuItemData,
            [name]: value || "",
        };
        setMenuItemData(updatedItem);
        if (onItemChanged)
        {
            onItemChanged(updatedItem);
        }
    };

    // Handle save button click
    const handleSave = () => {
        if (onUpdate) {
            onUpdate(menuItemData);
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
                    value={menuItemData.id == null ? "":menuItemData.id}
                    onChange={handleInputChange}
                    fullWidth
                    disabled={disabled}
                />
            </MenuItem>
            <MenuItem>
                <TextField
                    name="text"
                    label="Text"
                    value={menuItemData.text == null ? "":menuItemData.text}
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
                    <>
                        <br></br>
                        <br></br>
                        <br></br>

                        <Button
                            onClick={handleOpenSearch}
                            color="primary"
                            variant="contained"
                        >
                            Add Page
                        </Button>
                        <br></br>
                        <br></br>
                    </>
                )}
            </MenuItem>
            <MenuItem>
                {disabled && menuItemData.articleId != null && (
                    <TextField
                        name="articleName"
                        label="Article Name"
                        value={menuItemData.articleName}
                        onChange={handleInputChange}
                        fullWidth
                        disabled={disabled}
                    />
                )}
                {!disabled && !menuItemData.articleId != null && (
                    <>
                        <br></br>
                        <br></br>
                        <br></br>
                        <Button
                            color="primary"
                            variant="contained"
                        >
                            {/*Add Article*/}
                        </Button>
                    </>
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

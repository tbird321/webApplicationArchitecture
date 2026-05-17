import React,{ useState, useEffect, useCallback } from "react";
import Button from "@mui/material/Button";
import TextField from "@mui/material/TextField";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import Stack from "@mui/material/Stack";
import Autocomplete from "@mui/material/Autocomplete";

const defaultSelector = { id: null, name: "", type: "", stylingElements: [] };

const SelectorModal = ({ saveSelector, currentSelector, editMode, onSetEdit, selectorTypeOptions }) => {
    const [tempSelector, setTempSelector] = useState(defaultSelector);

    useEffect(() => {
        setTempSelector(editMode ? currentSelector : defaultSelector);
    }, [editMode, currentSelector]);

    // double function calls passsing field down to the second function so its available
    const handleInputChange = (field) => (event) => {
        setTempSelector({ ...tempSelector, [field]: event.target.value });
    };
    const handleAutocompleteTypeChange = (event, newValue) => {
        setTempSelector({ ...tempSelector, type: newValue });
    };
    const handleSave = useCallback(() => {
        if (typeof saveSelector === "function") {
            saveSelector(tempSelector);
            onSetEdit(false);
            setTempSelector(defaultSelector);
        } else {
            console.error("saveSelector is not a function");
        }
    }, [saveSelector, tempSelector, onSetEdit]);

    return (
        <Box>
            <Typography id="modal-modal-title" variant="h6" component="h2">
                <Stack spacing={2} sx={{ width: 400 }}>
                    <TextField
                        variant="standard"
                        type="text"
                        label="Name"
                        value={tempSelector.name || ""}
                        onChange={handleInputChange("name")}
                    />
                    <Autocomplete
                        id="selector-Type"
                        options={selectorTypeOptions??[]}
                        value={tempSelector.type || null} // Ensure value is null if type is not set
                        onChange={handleAutocompleteTypeChange}
                        getOptionLabel={(option) => option.label}
                        renderInput={(params) => <TextField {...params} label="Type" variant="standard" />}
                    />
                    <Button onClick={handleSave}>Save</Button>
                </Stack>
            </Typography>
        </Box>
    );
};

export default SelectorModal;

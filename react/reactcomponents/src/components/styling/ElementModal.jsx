import React, {useState, useEffect} from "react";
import  Button  from "@mui/material/Button";
import TextField from "@mui/material/TextField";
import Box from "@mui/material/Box";
import Autocomplete from "@mui/material/Autocomplete";
import Typography from "@mui/material/Typography";
import Stack from "@mui/material/Stack";
import { ChromePicker } from "react-color";
import availableFonts from "./AvailableFonts";
import stylingConstants from "./StylingConstants";
import availableCSSAttributes from "./AvailableCSSAttributes";


const ElementModal = ({newStyleElement,onElementSave,editMode,onSetEditMode, uniqueID})=> {
    //newStyleElement.attValue="15px"
    const [tempElement, setTempElement] = useState(newStyleElement);
    const [selectedColor, setSelectedColor] = useState(newStyleElement.attValue);
    const [sizeUnit, setSizeUnit] = useState("px");
    const [sizeValue, setSizeValue] = useState("");
    const defaultElement=
            {
                "id": null,
                "attName": "",
                "attValue": "",
                "attType": {label: "", value: ""},
            };
    const handleSizeValueChange = (e) => {
        const newValue = e.target.value;
        // Check if the new value is an integer
        if (/^\d*$/.test(newValue)) {
            // If it's an integer, update the state
            setSizeValue(newValue);
            setTempElement({ ...tempElement, attValue: `${newValue}${sizeUnit}` });
        } else {
            // If not, reset to an empty string or a default valid value
            setSizeValue("");
            setTempElement({ ...tempElement, attValue: `0${sizeUnit}` });
        }
    };

    useEffect(() => {
        // Check if newStyleElement.attValue is defined and is a string
        if (newStyleElement.attValue && typeof newStyleElement.attValue === "string") {
            // Extract the numeric part from the string
            const numericValue = newStyleElement.attValue.match(/\d+/);

            // Check if numericValue is not null
            if (numericValue) {
                setSizeValue(numericValue[0]);
            }
        }
    }, [newStyleElement.attValue]);


    const handleSizeUnitChange = (event, newValue) => {
        setSizeUnit(newValue); // Update sizeUnit state
        setTempElement({ ...tempElement, attValue: `${sizeValue}${newValue}` });
    };
    const handleSetEditMode = (data) => {
        onSetEditMode(data);
    };
    const handleElementSave = (data) => {
        onElementSave(data);
    };

    const handleAutocompleteNameChange = (field) => (event, newValue) => {
        if (field === "attName") {
            const selectedAttribute = availableCSSAttributes.find(attr => attr.label === newValue);

            let newType = null;
            if (selectedAttribute) {
                // Check if the attribute is a dropdown and set newType to the key (not the array)
                newType = selectedAttribute.type === "dropdown" ? getKeyForDropType(selectedAttribute.dropType) : selectedAttribute.type;
                setTempElement({ ...tempElement, attName: selectedAttribute.name, attType: newType });
            }
        } else {
            setTempElement({ ...tempElement, [field]: newValue });
        }
    };

    // Helper function to get the key for a given dropType (assuming dropType is an array)
    const getKeyForDropType = (dropType) => {
        // Iterate through stylingConstants to find the key that matches the dropType array
        for (const key in stylingConstants) {
            if (stylingConstants[key] === dropType) {
                return key; // Return the key that corresponds to the dropType array
            }
        }
        return null; // Return null if no matching key is found
    };



    const handleSaveButton =(event) => {
        tempElement.id = uniqueID();
        handleElementSave(tempElement);
        setTempElement(defaultElement);

    };
    const handleEditSaveButton =(event) => {
        handleElementSave(tempElement);
        handleSetEditMode(false);
        setTempElement(defaultElement);

    };

    const handleColorChange = (color) => {
        setSelectedColor(color.hex); // Update the selected color
        setTempElement((prevTempElement) => ({
            ...prevTempElement,
            attValue: color.hex,
        }));
    };


    const findLabelFromName = (name) => {
        const attribute = availableCSSAttributes.find(attr => attr.name === name);
        return attribute ? attribute.label : null;
    };
    const renderColorPicker = () => (
        <ChromePicker color={selectedColor} onChangeComplete={handleColorChange} />
    );

    const renderSizeInput = () => (
        <>
            <TextField
                type="text"
                id="size-value"
                label="Size Value"
                variant="standard"
                value={sizeValue}
                onChange={handleSizeValueChange}
            />
            <Autocomplete
                id="size-unit"
                options={stylingConstants.availableSizeUnits ?? []}
                value={sizeUnit}
                onChange={handleSizeUnitChange}
                renderInput={(params) => {
                    return (
                        <TextField
                            {...params}
                            label="Unit"
                            variant="standard"
                        />
                    );
                }}
            />
        </>
    );

    const renderFontPicker = () => (
        <Autocomplete
            id="font-picker"
            options={availableFonts??[]}
            value={tempElement?.attValue === "" ? "Arial" : tempElement?.attValue}
            onChange={handleAutocompleteNameChange("attValue")}
            renderInput={(params) => (
                <TextField
                    {...params}
                    type="text"
                    id="Font-standard-basic"
                    label="Font"
                    variant="standard"
                    InputProps={{
                        ...params.InputProps,
                        style: { fontFamily: tempElement?.attValue },
                    }}
                />
            )}
            renderOption={(props, option) => (
                <li {...props} key={option} style={{ fontFamily: option }}>
                    {option}
                </li>
            )}
        />
    );

    const renderDropdownInput = (selectedAttribute) => {
        const dropOptions = selectedAttribute.dropType;
        return (
            <Autocomplete
                id={`${selectedAttribute.dropType}-picker`}
                options={dropOptions??[]}
                value={tempElement.attValue}
                onChange={handleAutocompleteNameChange("attValue")}
                renderInput={(params) => (
                    <TextField
                        {...params}
                        label={selectedAttribute.label}
                        variant="standard"
                    />
                )}
            />
        );
    };

    const renderAttributeInput = () => {
        const selectedAttribute = availableCSSAttributes.find(attr => attr.name === tempElement.attName);

        if (!selectedAttribute) return null;

        switch (selectedAttribute.type) {
        case "color":
            return renderColorPicker();
        case "size":
            return renderSizeInput();
        case "font":
            return renderFontPicker();
        case "dropdown":
            return renderDropdownInput(selectedAttribute);
        default:
            return null;
        }
    };    return (
        <Box >
            <Typography id="modal-modal-title" variant="h6" component="h2">
                <Stack spacing={2} sx={{ width: 400 }}>
                    <Autocomplete
                        id="attName-autocomplete"
                        options={availableCSSAttributes?.map((attribute) => attribute.label)??[]}
                        value={findLabelFromName(tempElement?.attName)}
                        onChange={(e,newValue)=>{
                            handleAutocompleteNameChange("attName")(e,newValue);
                        }}
                        renderInput={(params) => (
                            <TextField
                                {...params}
                                label="Attribute Name"
                                variant="standard"
                            />
                        )}
                    />
                    {renderAttributeInput()}
                    {
                        editMode ? (
                            <Button onClick={handleEditSaveButton}>Save Element</Button>
                        ) : (
                            <Button onClick={handleSaveButton}>Save Element</Button>
                        )
                    }


                </Stack>
            </Typography>
        </Box>
    );
};


export default ElementModal;

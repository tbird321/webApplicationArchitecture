import React, {useState, useMemo, useEffect} from "react";
import  Button  from "@mui/material/Button";
import { DataGrid } from "@mui/x-data-grid";
import ModalDialog from "../ModalDialog";
import ElementModal from "./ElementModal";
import availableCSSAttributes from "./AvailableCSSAttributes";



const DEFAULT_STYLE_ELEMENT = {
    id: "",
    attName: "",
    attValue: "",
    attType: ""
};
const StylingGrid = ({attColumns,attTypes,currentSelector,onSetCurrentSelector,onUpdateSelectors,pageSize, uniqueID})=> {
    const [styleModal, setStyleModal] = useState(false);
    const [newCurrentSelector, setNewCurrentSelector] = useState(currentSelector);
    const [editMode, setEditMode] = useState(false);
    const [newStyleElement, setNewStyleElement] = useState(DEFAULT_STYLE_ELEMENT);

    useEffect(() => {
        setNewCurrentSelector(currentSelector);
    }, [currentSelector]);

    const handleUpdateSelectors = (data) => {
        onUpdateSelectors(data);
    };
    const handleSetCurrentSelector = (data) => {
        onSetCurrentSelector(data);
    };
    const handleClose = () => {
        setStyleModal(false);
        setEditMode(false);
    };
    const handleSetEditMode = () => setEditMode();

    const handleRowClick = (selectionModel)=>{
        setNewStyleElement({...selectionModel.row});
        setEditMode(true);
        setStyleModal(true);
    };
    const handleCreateStyleElement = () => {
        setNewStyleElement(DEFAULT_STYLE_ELEMENT);
        setEditMode(false);
        setStyleModal(true);
    };

    const handleElementSave = (newElement) => {
        let selectorCopy = { ...newCurrentSelector };

        if (editMode) {
            selectorCopy.stylingElements = selectorCopy.stylingElements?.map(element =>
                element.id === newElement.id ? newElement : element
            );
        } else {
            selectorCopy.stylingElements = [...selectorCopy.stylingElements, newElement];
        }
        handleSetCurrentSelector(selectorCopy);
        setNewCurrentSelector(selectorCopy);
        handleUpdateSelectors(selectorCopy);
        setStyleModal(false);
        setNewStyleElement({
            id: "",
            attName: "",
            attValue: "",
            attType: "",
        });
    };

    const findLabelFromName = (attName) => {
        const attribute = availableCSSAttributes.find(attr => attr.name === attName);
        return attribute ? attribute.label : null;
    };


    const dataGridColumns = useMemo(() => {
        const handleDelete = (id, event) => {
            event.stopPropagation();
            let updatedStylingElements = newCurrentSelector.stylingElements.filter(element => element.id !== id);
            let updatedSelector = { ...newCurrentSelector, stylingElements: updatedStylingElements };
            setNewCurrentSelector(updatedSelector);
            handleSetCurrentSelector(updatedSelector);
        };

        const generateColumns = (attColumns) => {
            return attColumns?.map(column => {
                let columnDef = {
                    field: column.field,
                    headerName: column.Header,
                    flex: 1.5,
                    type: column.Type,
                };

                if (column.field === "attName") {
                    columnDef.valueGetter = (params) => findLabelFromName(params.row.attName);
                }

                if (column.field === "attType") {
                    columnDef.renderCell = (params) => params.row.attType ? params.row.attType.label : "";
                }

                return columnDef;
            }).concat({
                field: "delete",
                headerName: "",
                flex: 1,
                renderCell: (params) => (
                    <Button color="error" onClick={(event) => handleDelete(params.row.id, event)}>
                        Delete
                    </Button>
                ),
            });
        };

        return generateColumns(attColumns);
    }, [attColumns, newCurrentSelector, handleSetCurrentSelector]);

    return (
        <>
            <div>Style Elements for {newCurrentSelector.name}</div>
            <DataGrid
                sx={{"--unstable_DataGrid-radius": "0"}}
                rows={newCurrentSelector.stylingElements == null?[]:newCurrentSelector.stylingElements}
                columns={dataGridColumns??[]}
                initialState={{}}
                onRowClick={handleRowClick}
                pageSizeOptions={pageSize??[]}
            />
            <Button onClick={handleCreateStyleElement}>New Style Element</Button>

            <ModalDialog open={styleModal} onClose={handleClose} aria-labelledby="modal-modal-title" aria-describedby="modal-modal-description" >
                <ElementModal
                    newStyleElement={newStyleElement}
                    onElementSave={handleElementSave}
                    attTypes={attTypes}
                    editMode={editMode}
                    uniqueID={uniqueID}
                    onSetEditMode={handleSetEditMode}
                />
            </ModalDialog>

        </>
    );
};
export default StylingGrid;


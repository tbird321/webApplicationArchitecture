import React, {useState, useEffect} from "react";
import  Button  from "@mui/material/Button";
import Box from "@mui/material/Box";
import { DataGrid } from "@mui/x-data-grid";
import Grid from "@mui/material/Unstable_Grid2"; // Grid version 2
import StylingGrid from "./StylingGrid";
import SelectorModal from "./SelectorModal";
import ModalDialog from "../ModalDialog";
import "./ThemeBuilder.css";

const attColumns = [
    { field: "attName", Header: "Attribute Name", Type: "Text" },
    { field: "attValue", Header: "Attribute Value", Type: "Text" },
];
const attTypes = [
    { label: "Color", value: "color" },
    { label: "Size", value: "size" },
    { label: "Font", value: "font" },
];
const selectorTypeOptions = [
    { label: "Class", value: "class" },
    { label: "Id", value: "id" },
    { label: "Tag", value: "tag" },
];
const defaultSelector = {
    "id": null,
    "name": "",
    "type": "",
    "stylingElements": []
};
const ThemeBuilder = ({columns,selectors,pageSize,onSaveSelectors})=> {

    const [selectorModalOpen, setSelectorModalOpen] = useState(false);
    const [editMode, setEditMode] = useState(false);
    const [selectorsLoaded, setSelectorsLoaded] = useState(selectors);
    const [showStylingGrid, setshowStylingGrid] = useState(false);

    const [currentSelector, setCurrentSelector] = useState(defaultSelector);
    useEffect(() => {
        setSelectorsLoaded(selectors);
    }, [selectors]);

    const handleEditSelector = () => {
        setSelectorModalOpen(true);
        setEditMode(true);
    };
    const handleNewSelector = () => {
        setSelectorModalOpen(true);
        setEditMode(false);
    };
    const handleSetEdit = () => {setEditMode(true);};
    const handleClose = () =>
    {
        setSelectorModalOpen(false);
        setEditMode(false);
    };
    const handleDelete = (id) => {
        const updatedSelectors = selectorsLoaded?.filter(article => article.id !== id);
        if(updatedSelectors.length===0){
            setSelectorsLoaded([]);

        }else{
            setSelectorsLoaded([...updatedSelectors]);
        }
        setEditMode(false);
        if(id===currentSelector.id){
            setshowStylingGrid(false);
            setCurrentSelector({...defaultSelector});
        }

    };
    const handleUpdateSelectors = (selectorCopy) => {
        const updatedSelectors = selectorsLoaded?.map(selector =>
            selector.id === selectorCopy.id ? selectorCopy : selector
        );
        setSelectorsLoaded(updatedSelectors);
        if (currentSelector.id === selectorCopy.id) {
            setCurrentSelector(selectorCopy);
        }
    };

    const handleSetCurrentSelector = (input)=> {
        setCurrentSelector(input);
        saveSelector(input);
    };
    const handleRowClick = (selectionModel)=>{
        if(selectionModel.row === currentSelector){
            setshowStylingGrid(false);
            setCurrentSelector({...defaultSelector});

        }else{
            setshowStylingGrid(true);
            setCurrentSelector(selectionModel.row);
        }
    };
    const saveSelector = (tempSelector) => {
        if (tempSelector.id) {
            setSelectorsLoaded([...selectorsLoaded?.map(selector =>
                selector.id === tempSelector.id ? tempSelector : selector
            )]);
        } else {
            const newSelector = { ...tempSelector, id: uniqueID() };
            setSelectorsLoaded([...selectorsLoaded, newSelector]);
        }
        console.log(selectorsLoaded);
        setSelectorModalOpen(false);
    };

    const generateColumns = (columns) => {
        const columnDefinitions = columns?.map(column => {
            let columnDef = {
                field: column.field,
                headerName: column.Header,
                flex: 1.5,
                type: column.Type,
            };
            if (column.field === "type") {
                columnDef.renderCell = (params) => {
                    return params.row.type ? params.row.type.label : "";
                };
            }
            return columnDef;
        });
        columnDefinitions.push({
            field: "delete",
            headerName: "",
            flex: 1,
            renderCell: (params) => (
                <Button  color="error" onClick={() => handleDelete(params.row.id)}> Delete </Button>
            ),
        });

        return columnDefinitions;
    };
    const uniqueID = () => {
        return Date.now();
    };
    const dataGridColumns = generateColumns(columns);
    const calculateRowHeight = (params) => {
        const baseHeight = 54; // base height for rows
        const extraHeightPerTag = 34; // extra height for each tag beyond a certain number
        const maxTagsInSingleLine = 1; // adjust as per your UI design

        const KeywordCount = params.model?.keywords ? params.model.keywords.length : 0;
        const TopicCount = params.model?.topics ? params.model.topics.length : 0;
        // Calculate additional height needed for keywords and topics separately
        const additionalHeightForKeywords = KeywordCount > maxTagsInSingleLine ? Math.ceil((KeywordCount - maxTagsInSingleLine) / maxTagsInSingleLine) * extraHeightPerTag : 0;
        const additionalHeightForTopics = TopicCount > maxTagsInSingleLine ? Math.ceil((TopicCount - maxTagsInSingleLine) / maxTagsInSingleLine) * extraHeightPerTag : 0;

        // Use the greater of the two calculated heights
        const totalAdditionalHeight = Math.max(additionalHeightForKeywords, additionalHeightForTopics);

        return baseHeight + totalAdditionalHeight;
    };


    const handleSaveTheme=(selectorData)=>{
        if (onSaveSelectors)
        {
            onSaveSelectors(selectorData);
        }
    };

    return (
        <div className='minimumSize'>
            <Grid container rowSpacing={1} columnSpacing={{ xs: 1, sm: 2, md: 3 }}>
                <Box sx={{ width: "45%" }}>
                    <div>Selectors</div>
                    <DataGrid
                        sx={{ "--unstable_DataGrid-radius": "0" }}
                        rows={selectorsLoaded == null ? [] : selectorsLoaded}
                        columns={dataGridColumns}
                        getRowHeight={calculateRowHeight}
                        initialState={{}}
                        onRowClick={handleRowClick}
                        pageSizeOptions={pageSize??[]}
                    />
                    <Button onClick={() => { handleNewSelector();}}>New Selector</Button>
                    {showStylingGrid &&
                        <Button  onClick ={()=> {handleEditSelector();}}>Edit Selector</Button>
                    }
                    <Button  onClick ={()=> {handleSaveTheme({...selectorsLoaded});}}>Save Theme</Button>
                </Box>
                <Box sx={{ width: "45%" }}>
                    {showStylingGrid &&
                        <StylingGrid
                            currentSelector={currentSelector}
                            onSetCurrentSelector= {handleSetCurrentSelector}
                            attColumns={attColumns}
                            attTypes= {attTypes}
                            pageSize={pageSize}
                            uniqueID={uniqueID}
                            onUpdateSelectors={handleUpdateSelectors}
                        />
                    }
                </Box>
                <ModalDialog open={selectorModalOpen} onClose={handleClose} aria-labelledby="modal-modal-title" aria-describedby="modal-modal-description" >
                    {
                        selectorModalOpen &&
                        <SelectorModal
                            saveSelector={saveSelector}
                            currentSelector={currentSelector}
                            editMode={editMode}
                            onSetEdit={handleSetEdit}
                            selectorTypeOptions={selectorTypeOptions??[]}
                        />
                    }
                </ModalDialog>
            </Grid>
        </div>
    );
};
export default ThemeBuilder;

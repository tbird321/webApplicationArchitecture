
import React, { useState, useEffect } from "react";
import "./SearchGrid.css";
import  Button  from "@mui/material/Button";
import TextField from "@mui/material/TextField";
import Box from "@mui/material/Box";
import { DataGrid } from "@mui/x-data-grid";
import Grid from "@mui/material/Unstable_Grid2"; // Grid version 2
import { isPromise } from "../utils";
import Autocomplete from "@mui/material/Autocomplete";
import Chip from "@mui/material/Chip";

const SearchGrid = ({ columns, pageSize, pageSizeOptions, onSearch, onRowSelect, keywordOptions, topicsOptions, items, searchType }) => {
    const [currentItems, setCurrentItems] = useState(items ?? []);
    const [searchCriteria, setSearchCriteria] = useState({});

    // Handle backward compatibility - if pageSize is an array, treat it as pageSizeOptions
    const actualPageSizeOptions = Array.isArray(pageSize) ? pageSize : (pageSizeOptions ?? [10, 25, 50, 100]);
    const actualPageSize = Array.isArray(pageSize) ? 25 : (pageSize ?? 25);

    const handleSearch = () => {
        onSearch({ ...searchCriteria });
    };
    const handleSelect = (rows) => {
        onRowSelect(rows);
    };
    const handleOnitemChange = (field, value) => {
        setCurrentItems({ ...currentItems, [field]: value });
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
    const handleStateChange = (fieldname)=>(e)=>{
        var newState={...searchCriteria};
        newState[fieldname]=e.target.value;
        setSearchCriteria(newState);
    };

    return (
        <div>
            <Box maxWidth="md">
                <Grid container rowSpacing={1} columnSpacing={{ xs: 1, sm: 2, md: 3 }}>
                    <Grid item="true" xs="auto">
                        <TextField
                            type="name"
                            id="name standard-basic"
                            label="Name"
                            variant="standard"
                            value={searchCriteria.Name}
                            onChange={handleStateChange("Name")}
                        />
                    </Grid>
                    <Grid item="true" xs>
                        <TextField
                            type="description"
                            id="description standard-basic"
                            label="Description"
                            variant="standard"
                            value={searchCriteria.Description}
                            onChange={handleStateChange("Description")}
                        />
                    </Grid>
                    {searchType == "page" &&
                    <>
                        <Grid item="true" xs>
                            <Autocomplete
                                multiple
                                id="tags-standard"
                                options={keywordOptions??[]}
                                defaultValue={Array.isArray(items.keywords) ? items.keywords : []}
                                renderTags={(value, getTagProps) => renderTags(setCurrentItems)(value, getTagProps)}
                                onChange={(e, newValue) => handleOnitemChange("keywords", newValue)}
                                renderInput={(params) => (
                                    <TextField
                                        {...params}
                                        type="text"
                                        id="Keywords-standard-basic"
                                        label="Keywords"
                                        variant="standard"
                                    />
                                )}
                            />
                        </Grid>
                        <Grid item="true" xs>
                            <Autocomplete
                                multiple
                                id="tags-standard"
                                options={topicsOptions??[]}
                                defaultValue={Array.isArray(currentItems.topics) ? currentItems.topics : []}
                                renderTags={(value, getTagProps) => renderTags(setCurrentItems)(value, getTagProps)}
                                onChange={(e, newValue) => handleOnitemChange("topics", newValue)}
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
                        </Grid>
                    </>
                    }
                    <Button
                        color="primary"
                        variant="contained"
                        className="search-button"
                        onClick={handleSearch}>Search
                    </Button>
                </Grid>
            </Box>
            <br></br>
            <Box sx={{ height: 400, width: "100%" }}>
                <DataGrid
                    sx={{ "--unstable_DataGrid-radius": "0" }}
                    rows={items}
                    columns={columns??[]}
                    initialState={{
                        pagination: {
                            pageSize: actualPageSize,
                        },
                    }}
                    onRowClick={handleSelect}
                    onSelectionModelChange={handleSelect}
                    selectionModel={[]}
                    pageSizeOptions={actualPageSizeOptions}
                />
            </Box>
        </div>
    );
};

export default SearchGrid;

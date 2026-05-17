import React, { useState, useEffect, useRef, useCallback } from "react";
import  Button  from "@mui/material/Button";
import Box from "@mui/material/Box";
import { DataGrid } from "@mui/x-data-grid";
import Chip from "@mui/material/Chip";
import ModalDialog from "../ModalDialog";
import ArticleModal from "./ArticleModal";
import { isPromise } from "../utils";

import { SearchGrid } from "../data";
import PageFormFields from "./PageFormFields";

const CreateEditPage = ({ keywordOptions, topicsOptions, styleSheets, columns, pageData, onSave,onArticleLookup, pageSize, uniqueID,imageList,onArticleUpdated  }) => {
    const [articlesLoad, setArticlesLoad] = useState([...pageData?.articles??[]]);
    const [currentPage, setCurrentPage] = useState({...pageData});
    const [editArticle, setEditArticle] = useState(null);
    const [openArticle, setOpenArticle] = useState(false);
    const [tempArticles, setTempArticles] = useState([]);
    const [openArticleSearch, setOpenArticleSearch] = useState(false);
    const [modalDirty, setModalDirty] = useState(false);
    const eventListeners = useRef([]);
    const dialogStyle={width:"95%"};

    const handleNew = () =>{
        setEditArticle(null);
        setOpenArticle(true);
    };
    const handleArticleSearchClose =()=>{
        setOpenArticleSearch(false);
        setModalDirty(false);
    };
    const handleArticleLookup =()=>{
        setOpenArticleSearch(true);
    };
    const handleArticleRowClick = (selectionModel)=>{
        console.log(selectionModel);
        setOpenArticleSearch(false);
        const articleExists = articlesLoad.filter(article => article.id === selectionModel.row.id);
        if (articleExists?.length==0)
        {
            var updatedArticles=[...articlesLoad,{...selectionModel.row,id:uniqueID()}];
            setArticlesLoad(updatedArticles);
        }
    };
    const handleArticleSearch =(searchCrit)=>{
        if (onArticleLookup)
        {
            var searchResults=onArticleLookup(searchCrit);
            setTempArticles([...searchResults]);
            return searchResults;
        }else
        {
            return [];
        }
    };
    const handleSelect = (newRowSelectionModel) => {
        const selectedArticle = articlesLoad.filter(article => article.id == newRowSelectionModel[0]);
        if (selectedArticle && selectedArticle.length>0) {
            setEditArticle(selectedArticle[0]);
            setOpenArticle(true);
        }
    };
    const handleOnSave= (data)=>{
        if (onSave)
        {
            //reset and set sequence numbers before sending to save...
            let sequence = 0; // Start sequence from 0
            const updatedArticles = data.articles?.map(article => {
                sequence += 5; // Increment sequence by 5
                return { ...article, sequence_no: sequence };
            });
            const newData = { ...data, articles: updatedArticles };
            onSave(newData);
        }
    };

    const handleClose = () => {
        setOpenArticle(false);
        setModalDirty(false);
    };

    const handleArticleDelete = (event,row) => {
        event.stopPropagation();
        console.log("Button clicked on row:", row);
        const updatedArticles = articlesLoad.filter(article => article.id !== row.id);
        setArticlesLoad([...updatedArticles]);
    };
    const handlePageChange = (field, value) => {
        setCurrentPage({ ...currentPage, [field]: value });
    };

    const updateArticles = (input) => {
        let itemFound = false;
        let newArticles = articlesLoad.map((item) => {
            if (item.id == input.id) {
                itemFound = true;
                return input;
            } else {
                return item;
            }
        });
        if (!itemFound) {
            newArticles.push(input);
        }
        setArticlesLoad(newArticles);
        setOpenArticle(false);
    };

    const handleArticleUpdated = async (input) => {
        if (onArticleUpdated) {
            var result = onArticleUpdated(input);
            if (isPromise(result)) {
                onArticleUpdated(input).then((searchResults) => {
                    updateArticles(searchResults);
                });
            } else {
                updateArticles(result);
            }
        }
    };

    const renderCellWithScrollbar = (params) => {
        return (
            <Box sx={{
                maxHeight: "100px",
                overflowY: "auto",
                overflowX: "hidden",
                width: "100%",
                display: "flex",
                flexDirection: "column", // Stack chips vertically
                gap: "5px"  // Optional: adds space between chips
            }}>
                {params.value?.map((item, index) => (
                    <Chip key={index} label={item} variant="outlined" />
                ))}
            </Box>
        );
    };
    const generateColumns1 = useCallback((columns) => {
        const columnDefinitions = columns?.map(column => {
            let columnDef = {
                field: column.field,
                headerName: column.Header,
                flex: 1.5,
                type: column.Type,
            };
            if (column.Type === "Tags" || column.Type === "keywords") {
                columnDef = {
                    ...columnDef,
                    renderCell: renderCellWithScrollbar
                };
            }

            return columnDef;
        });
        columnDefinitions.push({
            field: "delete",
            headerName: "",
            flex: 1,
            renderCell: (params) => (
                <Button color="error" onClick={(event) => handleArticleDelete(event, params.row)}> Delete </Button>
            ),
        });

        return columnDefinitions;
    }, [articlesLoad]); // Dependencies for useCallback

    const generateColumns = (columns) => {
        const columnDefinitions = columns?.map(column => {
            let columnDef = {
                field: column.field,
                headerName: column.Header,
                flex: 1.5,
                type: column.Type,
            };
            if (column.Type === "Tags"|| column.Type === "keywords") {
                columnDef = {
                    ...columnDef,
                    renderCell: renderCellWithScrollbar
                };
            }

            return columnDef;
        });
        columnDefinitions.push({
            field: "delete",
            headerName: "",
            flex: 1,
            renderCell: (params) => (
                <Button  color="error" onClick={(event) => handleArticleDelete(event,params.row)}> Delete </Button>
            ),
        });

        return columnDefinitions;
    };

    const calculateRowHeight = (params) => {
        const baseHeight = 54; // base height for rows
        const extraHeightPerTag = 34; // extra height for each tag beyond a certain number
        const maxTagsInSingleLine = 1; // adjust as per your UI design

        const KeywordCount = params.model.keywords?.length> 3 ? 3 : params.model.keywords.length;
        const TopicCount = params.model.topics?.length > 3 ? 3 : params.model.topics.length;

        // Calculate additional height needed for keywords and topics separately
        const additionalHeightForKeywords = KeywordCount > maxTagsInSingleLine ? Math.ceil((KeywordCount - maxTagsInSingleLine) / maxTagsInSingleLine) * extraHeightPerTag : 0;
        const additionalHeightForTopics = TopicCount > maxTagsInSingleLine ? Math.ceil((TopicCount - maxTagsInSingleLine) / maxTagsInSingleLine) * extraHeightPerTag : 0;

        // Use the greater of the two calculated heights
        const totalAdditionalHeight = Math.max(additionalHeightForKeywords, additionalHeightForTopics);

        return baseHeight + totalAdditionalHeight;
    };

    //Problems with Drag and Drop shut it off for now
    /*
    useEffect(() => {
        if (articlesLoad?.length>0)
        {
            console.log("setup timer");
            //Wait until full react render pipeline is complete... then setup drag and drop renders...
            setTimeout(() => {
                const draggableItems = document.querySelectorAll(".MuiDataGrid-row");
                const setDragImageStyle = (dragImage, originalContent) => {
                    dragImage.textContent = originalContent; // Copy content for visual similarity
                    Object.assign(dragImage.style, {
                        backgroundColor: "grey",
                        color: "white",
                        padding: "10px",
                        borderRadius: "4px",
                        boxShadow: "0px 2px 10px rgba(0,0,0,0.5)",
                        position: "absolute",
                        top: "-100px" // Position off-screen
                    });
                };

                const onDragStart = (event) => {
                    const sourceItem = event.target;
                    event.dataTransfer.setData("sourceID", sourceItem.getAttribute("data-id"));
                    sourceItem.classList.add("dragging");

                    const dragImage = sourceItem.cloneNode(true);
                    setDragImageStyle(dragImage, sourceItem.textContent);
                    document.body.appendChild(dragImage);
                    event.dataTransfer.setDragImage(dragImage, 0, 0);

                    // Remove drag image from DOM immediately after setting it
                    setTimeout(() => document.body.removeChild(dragImage), 0);
                };


                const onDragEnd = (event) => {
                    event.target.classList.remove("dragging");
                };

                const onDragOver = (event) => {
                    event.preventDefault();
                };
                const cleanupEvents = () => {
                    draggableItems.forEach(item => {
                        item.removeEventListener("dragstart", onDragStart);
                        item.removeAttribute("data-dragstart-added");
                        item.removeEventListener("dragend", onDragEnd);
                        item.removeAttribute("data-dragend-added");
                        item.removeEventListener("dragover", onDragOver);
                        item.removeAttribute("data-dragover-added");
                        item.removeAttribute("data-drop-added");
                        //handle drop listeners differently - for they are dynamic listeners and thus need to be  mainained and removed in a reference
                        eventListeners.current.forEach(({ item, handler }) => {
                            item.removeEventListener("drop", handler);
                        });
                        eventListeners.current = [];
                    });
                };
                const onDrop = (event, item) => {
                    event.preventDefault();
                    event.target.classList.remove("dragging"); // Remove dragging class
                    const sourceId = +event.dataTransfer.getData("sourceID");
                    const targetId = +item.getAttribute("data-id");
                    const sourceIndex = articlesLoad.findIndex(article => article.id === sourceId);
                    const targetIndex = articlesLoad.findIndex(article => article.id === targetId);

                    if (sourceIndex < 0 || targetIndex < 0 || sourceIndex >= articlesLoad?.length || targetIndex >= articlesLoad?.length) {
                        return; // Exit if indexes are out of range
                    }
                    if (sourceIndex !== targetIndex) {
                        const updatedArticles = [...articlesLoad];
                        const [movedArticle] = updatedArticles.splice(sourceIndex, 1);
                        updatedArticles.splice(targetIndex, 0, movedArticle);
                        setArticlesLoad(updatedArticles); // Update state
                    }
                };
                draggableItems.forEach(item => {
                    const addEventListenerOnce = (item, eventType, handler, attribute) => {
                        if (!item.hasAttribute(attribute)) {
                            item.addEventListener(eventType, handler);
                            item.setAttribute(attribute, "true");
                        }
                    };
                    item.setAttribute("draggable", true);
                    addEventListenerOnce(item, "dragstart", onDragStart, "data-dragstart-added");
                    addEventListenerOnce(item, "dragend", onDragEnd, "data-dragend-added");
                    addEventListenerOnce(item, "dragover", onDragOver, "data-dragover-added");
                    // Check if the drop event listener has not been added yet
                    if (!item.hasAttribute("data-drop-added")) {
                        const myNewHandler=(event) => onDrop(event, item);
                        eventListeners.current.push({item:item,handler:myNewHandler});
                        item.addEventListener("drop", myNewHandler);
                        item.setAttribute("data-drop-added", "true");
                    }
                });

                // Clean-up function
                return () => {
                    cleanupEvents();
                };

            }, 100);
        }
    }, [articlesLoad]);
    */
    const handleArticleModalUpdated = () => {
        setModalDirty(true);
    };

    return (
        <>
            <PageFormFields
                currentPage={currentPage}
                onPageChange={handlePageChange}
                styleSheets={styleSheets}
                topicsOptions={topicsOptions??[]}
                keywordOptions={keywordOptions??[]}
                selectedLayout={pageData.layout}
            />
            <Button onClick={handleNew}>New Article</Button>
            <Button onClick={handleArticleLookup}>Lookup Article</Button>
            { articlesLoad?.length>0 &&
            <div id='mydropzone'>
                <DataGrid
                    sx={{ "--unstable_DataGrid-radius": "0" }}
                    rows={articlesLoad || []}
                    columns={generateColumns(columns)}
                    getRowHeight={calculateRowHeight}
                    initialState={{}}
                    pageSizeOptions={pageSize??[]}
                    onRowSelectionModelChange={(newRowSelectionModel) => {
                        handleSelect(newRowSelectionModel);
                    }}
                />
            </div>
            }
            <Button  onClick ={()=> {handleOnSave({...currentPage, articles:articlesLoad});}}>Save Page</Button>
            <ModalDialog open={openArticle} style={{width:"600px"}} onClose={handleClose} aria-labelledby="modal-modal-title" aria-describedby="modal-modal-description" >
                <ArticleModal
                    keywordOptions={keywordOptions??[]}
                    topicsOptions={topicsOptions??[]}
                    uniqueID={uniqueID}
                    onSave={handleArticleUpdated}
                    articlesLoad={articlesLoad}
                    editArticle={editArticle}
                    imageList={imageList}
                />
            </ModalDialog>
            <ModalDialog open={openArticleSearch} childDirty={modalDirty} onArticleUpdated={handleArticleModalUpdated} onClose={handleArticleSearchClose} dialogStyle={dialogStyle} aria-labelledby="modal-modal-title" aria-describedby="modal-modal-description" >
                <SearchGrid
                    onRowSelect={handleArticleRowClick}
                    onSearch= {handleArticleSearch}
                    pageSize={pageSize}
                    columns={generateColumns(columns)}
                    items={tempArticles == null?[]:tempArticles}
                    imageList={imageList}
                />
            </ModalDialog>
        </>
    );
};
export default CreateEditPage;

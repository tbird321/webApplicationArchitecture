import React, {useState, useEffect} from "react";
import  Button  from "@mui/material/Button";
import { CreateEditPage, ModalDialog, SearchGrid, isPromise } from '@tbirdcomponents/reactcomponents'; // Adjust the import path accordingly
import { useAppStateContext } from '../../hooks/appState/useAppStateContext'; // Adjust the import path accordingly

const PageSelector = ({ keywordOptions, topicsOptions, styleSheets, onSearch, onArticleSearch, articleColumns, pages , savePage, pageSize, onSave,layouts, imageList }) => {
    const childProps = { keywordOptions, topicsOptions, styleSheets, savePage, pageSize,columns:articleColumns,layouts };
    const [tempPages, setTempPages] = useState(pages);
    const [open, setOpen] = React.useState(false);
    const { WebSiteState } = useAppStateContext();
    const defaultNewPage = {
        "name": "page 1",
        "description": "page Description 1",
        "topics": [],
        "keywords": [],
        "layout": "Standard",
        "style": "style1",
        "articles": []
    };
    const [currentPage, setCurrentPage] = useState(defaultNewPage);

    const samplePageColumns = [
        { field: "name", Header: "Name", Type: "Text"},
        { field: "description", Header: "Description", Type: "Text"},
        { field: "topics", Header: "Topics", Type: "Tags"},
        { field: "keywords", Header: "Keywords", Type: "Tags"},
    ];
    const dialogStyle={width:"95%"};

    const handleSearch =(searchCrit)=>{
        if (onSearch) {
            searchCrit.websiteId = WebSiteState.websiteID();
            var results = onSearch(searchCrit);
            var searchResults = [];
            if (isPromise(results)) {

            } else {
                searchResults = [...results];
            }
            setTempPages(searchResults);
            return searchResults;
        }else
        {
            return [];
        }
    };


    const handleNewPageOpen =()=>{
        const newPage={...defaultNewPage};
        setCurrentPage(newPage);
        setOpen(true);
    };
    const handleClose = () => setOpen(false);
    const handleDelete = (id,event) => {
        event.stopPropagation();
        const updatedArticles = tempPages.filter(article => article.id !== id);
        setTempPages(updatedArticles);
    };
    const handleRowClick = (selectionModel) => {
        setCurrentPage(selectionModel.row);
        setOpen(true);
    };

    const generateColumns = (columns) => {
        const columnDefinitions = columns?.map(column => {
            let columnDef = {
                field: column.field,
                headerName: column.Header,
                flex: 1.5,
                type: column.Type,
            };
            return columnDef;
        });
        columnDefinitions.push({
            field: "delete",
            headerName: "",
            flex: 1,
            renderCell: (params) => (
                <Button  color="error" onClick={(e) => handleDelete(params.row.id, e)}> Delete </Button>
            ),
        });

        return columnDefinitions;
    };
    const dataGridColumns = generateColumns(samplePageColumns);
    const uniqueID = () => {
        return Date.now();
    };

    const handleSavePage = (newPage) => {
        let updatedPages;
        if (newPage.id) {
            // Edit existing page
            updatedPages = tempPages?.map(curPage =>
                curPage.id === newPage.id ? { ...newPage } : curPage
            );
        } else {
            // Add new page
            const newId = uniqueID(); // Generate a unique ID for the new page
            updatedPages = [...tempPages, { ...newPage, id: newId, websiteId: WebSiteState.websiteID() }];
        }
        setTempPages(updatedPages);
        setOpen(false);
        handleOnSave(newPage);
    };


    const handleOnSave = (data) => {
        if (onSave) {
            onSave(data);
        }
    };

    useEffect(()=>{
        setTempPages([...pages]);
    },[pages]);


    const handleArticleLookup = async (selectionCriteria)=>{
        //Handle Article Lookup
        if (onArticleSearch)
        {
            return  await onArticleSearch(selectionCriteria);
        }else
        {

            return [];
        }
    };

    return (
        <div className='minimumSize'>
            <Button onClick={handleNewPageOpen}>New Page</Button>
            <SearchGrid
                onRowSelect={handleRowClick}
                onSearch= {handleSearch}
                pageSize={pageSize}
                columns={dataGridColumns}
                items={tempPages == null?[]:tempPages}
                imageList={imageList}
            />
            <ModalDialog open={open} onClose={handleClose} dialogStyle={dialogStyle} aria-labelledby="modal-modal-title" aria-describedby="modal-modal-description" >
                <CreateEditPage
                    {...childProps}
                    pageData={currentPage}
                    onSave ={handleSavePage}
                    uniqueID = {uniqueID}
                    imageList={imageList}
                    onArticleLookup={handleArticleLookup}
                />
            </ModalDialog>
        </div>

    );
};
export default PageSelector;

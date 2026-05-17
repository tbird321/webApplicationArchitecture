import React, { useState, useMemo } from 'react';
import { Button, Typography } from '@mui/material';
import { ModalDialog, SearchGrid, CreateEditPage } from '@tbirdcomponents/reactcomponents';
import { useAppStateContext } from '../../hooks/appState/useAppStateContext'; // Assuming this is available
import './PageLookup.css';

function PageLookup({ showLookup, onSelected, onClose, currentPage }) {
    const { WebSiteState, DatabaseProcessing } = useAppStateContext(); // Use your utility hook
    const [criteria, setCriteria] = useState({
        name: '',
        keyword: '',
        layout: '',
        topic: '',
    });

    const [searchResults, setSearchResults] = useState([]);
    const [pageSize, setPageSize] = useState([5,10,20]); // Default page size
    const [imageList, setImageList] = useState([]); // Assuming you have an image list

    // Derive layouts, keywords, and topics from WebSiteState
    const pageKeywords = useMemo(() => WebSiteState.getPageKeywords() ?? [], [WebSiteState]);
    const pageTopics = useMemo(() => WebSiteState.getPageTopics() ?? [], [WebSiteState]);

    // Handle the search logic, using DatabaseProcessing to fetch pages
    const handleSearch = async () => {
        const searchCriteria = {
            name:criteria.name,
            keyword: criteria.keyword,
            layout: criteria.layout,
            topic: criteria.topic,
            WebsiteId: WebSiteState.websiteID(), // Ensure WebsiteId is included in search criteria
        };

        try {
            const pageInfo = await DatabaseProcessing.searchPage(searchCriteria);
            setSearchResults(pageInfo); // Set the results in the state to display
        } catch (error) {
            console.error('Error while searching pages:', error);
        }
    };

    // Handle selection of a result
    const handleRowClick = (page) => {
       
    };
    const handleDialogClose = () => {
        if (onClose) {
            onClose();
        };
    };

    const handlePageSelect = (page) => {
        if (onSelected) {
            onSelected(page);
        }
    };


    const dataGridColumns = [
        { field: 'name', headerName: 'Name', width: 150 },
        { field: 'layout', headerName: 'Layout', width: 150 },
        { field: 'keywords', headerName: 'Keywords', width: 300, valueGetter: (params) => params.row.keywords.join(', ') },
        {
            field: 'select', headerName: 'Select', width: 100, renderCell: (params) => (
                <Button onClick={() => handlePageSelect(params.row)}>Select</Button>
            )
        },
    ];

    return (
        <ModalDialog open={showLookup} onClose={handleDialogClose}>
            <div>
                <div>{currentPage}</div>
                <Typography variant="h6">Page Lookup</Typography>
                
                <div>
                    <SearchGrid
                        keywordOptions={pageKeywords ?? []}
                        topicsOptions={pageTopics ?? []}
                        searchType="page"
                        onRowSelect={handleRowClick}
                        onSearch={handleSearch}
                        pageSize={pageSize}
                        columns={dataGridColumns}
                        items={searchResults}
                        imageList={imageList}
                    />
                </div>                
            </div>
        </ModalDialog>
    );
}

export default PageLookup;
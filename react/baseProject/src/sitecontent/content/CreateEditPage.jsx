import React, { useState, useCallback } from "react";
import Button from "@mui/material/Button";
import Box from "@mui/material/Box";
// Removed DataGrid usage in favor of always-on drag-and-drop list
import Chip from "@mui/material/Chip";
import List from "@mui/material/List";
import ListItem from "@mui/material/ListItem";
import ListItemText from "@mui/material/ListItemText";
import Divider from "@mui/material/Divider";
import { ModalDialog, ArticleModal, SearchGrid, PageFormFields, isPromise } from '@tbirdcomponents/reactcomponents'; // Adjust the import path accordingly
import IconButton from "@mui/material/IconButton";
import ArrowUpwardIcon from "@mui/icons-material/ArrowUpward";
import ArrowDownwardIcon from "@mui/icons-material/ArrowDownward";
import DragIndicatorIcon from "@mui/icons-material/DragIndicator";
import VisibilityIcon from "@mui/icons-material/Visibility";
import { HtmlContentRenderer } from '@tbirdcomponents/reactcomponents';
import { useAppStateContext } from '../../hooks/appState/useAppStateContext';

const CreateEditPage = ({ keywordOptions, topicsOptions, styleSheets, columns, pageData, onSave, onArticleLookup, pageSize, uniqueID, imageList, onArticleUpdated }) => {
    const [articlesLoad, setArticlesLoad] = useState([...pageData?.articles ?? []]);
    const [currentPage, setCurrentPage] = useState({ ...pageData });
    const [editArticle, setEditArticle] = useState(null);
    const [openArticle, setOpenArticle] = useState(false);
    const [tempArticles, setTempArticles] = useState([]);
    const [openArticleSearch, setOpenArticleSearch] = useState(false);
    const [modalDirty, setModalDirty] = useState(false);
    const [draggedIndex, setDraggedIndex] = useState(null);
    // const eventListeners = useRef([]);
    const dialogStyle = { width: "95%" };
    const { FileProcessing, Authorization } = useAppStateContext();
    const [previewOpen, setPreviewOpen] = useState(false);
    const [previewTitle, setPreviewTitle] = useState('');
    const [previewHTML, setPreviewHTML] = useState('');
    const [pagePreviewOpen, setPagePreviewOpen] = useState(false);
    const [pagePreviewTitle, setPagePreviewTitle] = useState('');
    const [pagePreviewItems, setPagePreviewItems] = useState([]);

    const handleNew = () => {
        setEditArticle(null);
        setOpenArticle(true);
    };
    const handleArticleSearchClose = (event, reason) => {
        if (reason !== 'backdropClick') {
            setOpenArticleSearch(false);
            setModalDirty(false);
        } 
        event.stopPropagation();
    };
    const handleArticleLookup = () => {
        setOpenArticleSearch(true);
    };
    // Double-click no longer used since DataGrid was removed; keep for future if needed
    const handleArticleRowClick = (selectionModel) => {
        console.log(selectionModel);
        setOpenArticleSearch(false);
        const articleExists = articlesLoad.filter(article => article.id === selectionModel.row.id);
        selectionModel.row.keywords = selectionModel.row.keywords ?? [];
        selectionModel.row.topics = selectionModel.row.topics ?? [];
        if (articleExists?.length === 0) {
            var updatedArticles = [...articlesLoad, { ...selectionModel.row }];
            setArticlesLoad(updatedArticles);
        }
    };
    const handleArticleSearch = async (searchCrit) => {
        if (onArticleLookup) {
            var searchResults = await onArticleLookup(searchCrit);
            setTempArticles(searchResults);
            return searchResults;
        } else {
            return [];
        }
    };
    const handleSelect = (newRowSelectionModel) => {
        const selectedArticle = articlesLoad.filter(article => article.id === newRowSelectionModel[0]);
        if (selectedArticle && selectedArticle.length > 0) {
            setEditArticle(selectedArticle[0]);
            setOpenArticle(true);
        }
    };
    const handleOnSave = async (data) => {
        if (onSave) {
            // Normalize and reindex sequence in the SAME order as provided
            const normalized = Array.isArray(data.articles) ? [...data.articles] : [];
            // Honor current list order; if sequence_no already exists, sort by it first to be predictable
            normalized.sort((a, b) => (a.sequence_no ?? 0) - (b.sequence_no ?? 0));
            let sequence = 0;
            const updatedArticles = normalized.map(article => {
                sequence += 5; // gaps allow inserts later
                return { ...article, sequence_no: sequence };
            });
            const newData = { ...data, articles: updatedArticles };
            await onSave(newData);
        }
    };

    const handleClose = () => {
        setOpenArticle(false);
        setModalDirty(false);
    };

    const handleArticleDelete = useCallback((event, row) => {
        event.stopPropagation();
        console.log("Button clicked on row:", row);
        const updatedArticles = articlesLoad.filter(article => article.id !== row.id);
        setArticlesLoad(reindexArticles([...updatedArticles]));
    }, [articlesLoad]);

    const reindexArticles = useCallback((list) => {
        let sequence = 0;
        return list.map((article) => {
            sequence += 5;
            return { ...article, sequence_no: sequence };
        });
    }, []);

    // Removed arrow-based move handlers

    // Drag-and-drop handlers (standard behavior)
    const handleDragStart = useCallback((index) => (event) => {
        setDraggedIndex(index);
        try {
            event.dataTransfer.effectAllowed = 'move';
            event.dataTransfer.setData('text/plain', String(index));
        } catch (e) {
            // ignore
        }
    }, []);

    const handleDragOver = useCallback(() => (event) => {
        event.preventDefault();
        try {
            event.dataTransfer.dropEffect = 'move';
        } catch (e) {
            // ignore
        }
    }, []);

    const handleDrop = useCallback((index) => (event) => {
        event.preventDefault();
        let fromIndex = draggedIndex;
        if (fromIndex === null) {
            const data = event.dataTransfer.getData('text/plain');
            const parsed = parseInt(data, 10);
            fromIndex = Number.isNaN(parsed) ? null : parsed;
        }
        if (fromIndex === null || fromIndex === index) return;
        const newList = Array.from(articlesLoad);
        const [moved] = newList.splice(fromIndex, 1);
        newList.splice(index, 0, moved);
        setArticlesLoad(reindexArticles(newList));
        setDraggedIndex(null);
    }, [articlesLoad, draggedIndex, reindexArticles]);

    // Removed obsolete save order function from previous toggle-based UI

    const previewArticle = useCallback(async (row) => {
        try {
            const config = Authorization.getConfiguration();
            const rootPath = `websites/${config.Site.siteName}`;
            let html = '';
            if (row?.articlePath) {
                html = await FileProcessing.getFileData(`${rootPath}/articles`, row.articlePath);
            }
            setPreviewTitle(row?.name ?? 'Article Preview');
            setPreviewHTML(html || '<div>No content</div>');
            setPreviewOpen(true);
        } catch (e) {
            setPreviewTitle('Article Preview');
            setPreviewHTML('<div>Failed to load content</div>');
            setPreviewOpen(true);
        }
    }, [Authorization, FileProcessing]);

    const previewPage = useCallback(async () => {
        try {
            const config = Authorization.getConfiguration();
            const rootPath = `websites/${config.Site.siteName}`;
            const ordered = [...(articlesLoad || [])].sort((a, b) => (a.sequence_no ?? 0) - (b.sequence_no ?? 0));
            const items = await Promise.all(ordered.map(async (article) => {
                let html = '';
                try {
                    if (article?.articlePath) {
                        html = await FileProcessing.getFileData(`${rootPath}/articles`, article.articlePath);
                    }
                } catch (e) {
                    html = '<div>Failed to load content</div>';
                }
                return { id: article.id, name: article.name, html };
            }));
            setPagePreviewTitle(currentPage?.name || 'Page Preview');
            setPagePreviewItems(items);
            setPagePreviewOpen(true);
        } catch (e) {
            setPagePreviewTitle('Page Preview');
            setPagePreviewItems([]);
            setPagePreviewOpen(true);
        }
    }, [Authorization, FileProcessing, articlesLoad, currentPage?.name]);

    const handlePageChange = (field, value) => {
        setCurrentPage({ ...currentPage, [field]: value });
    };

    const updateArticles = async (input) => {
        let itemFound = false;
        let newArticles = articlesLoad.map((item) => {
            if (item.id === input.id) {
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
            var result = await onArticleUpdated(input);
            if (isPromise(result)) {
                result.then(async (searchResults) => {
                    await updateArticles(searchResults);
                });
            } else {
                await updateArticles(result);
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
    // const generateColumns1 = useCallback((columns) => {
    //     // deprecated
    // }, [handleArticleDelete]);

    const generateColumns = (columns) => {
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
        // Add preview control (used in search results grid)
        columnDefinitions.push({
            field: "preview",
            headerName: "Preview",
            width: 110,
            sortable: false,
            filterable: false,
            disableColumnMenu: true,
            renderCell: (params) => (
                <IconButton size="small" onClick={(e) => { e.stopPropagation(); previewArticle(params.row); }} aria-label="Preview">
                    <VisibilityIcon fontSize="inherit" />
                </IconButton>
            ),
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
    };

    // Removed row height calculation for DataGrid

    const handleArticleModalUpdated = () => {
        setModalDirty(true);
    };

    return (
        <>
            <PageFormFields
                currentPage={currentPage}
                onPageChange={handlePageChange}
                styleSheets={styleSheets}
                topicsOptions={topicsOptions ?? []}
                keywordOptions={keywordOptions ?? []}
                selectedLayout={pageData.layout}
            />
            <Button onClick={handleNew}>New Article</Button>
            <Button onClick={handleArticleLookup}>Lookup Article</Button>
            {articlesLoad?.length > 0 && (
                <Box sx={{ border: '1px solid #e0e0e0', borderRadius: 1, mt: 1 }}>
                    <List>
                        {(articlesLoad || []).map((article, index) => (
                            <React.Fragment key={article.id ?? index}>
                                <ListItem
                                    draggable
                                    onDragStart={handleDragStart(index)}
                                    onDragOver={handleDragOver(index)}
                                    onDrop={handleDrop(index)}
                                    sx={{ cursor: 'grab' }}
                                >
                                    <DragIndicatorIcon sx={{ mr: 1, color: 'text.secondary' }} />
                                    <ListItemText
                                        primary={article.name}
                                        secondary={article.description}
                                    />
                                    <Box sx={{ ml: 'auto', display: 'flex', alignItems: 'center', gap: 1 }}>
                                        <IconButton size="small" onClick={(e) => { e.stopPropagation(); previewArticle(article); }} aria-label="Preview">
                                            <VisibilityIcon fontSize="inherit" />
                                        </IconButton>
                                        <Button color="error" size="small" onClick={(event) => handleArticleDelete(event, article)}>Delete</Button>
                                    </Box>
                                </ListItem>
                                {index < (articlesLoad?.length ?? 0) - 1 && <Divider />}
                            </React.Fragment>
                        ))}
                    </List>
                </Box>
            )}
            <Box sx={{ display: 'flex', gap: 1, alignItems: 'center', mt: 1 }}>
                <Button onClick={() => { handleOnSave({ ...currentPage, articles: articlesLoad }); }}>Save Page</Button>
                <Button startIcon={<VisibilityIcon />} onClick={previewPage}>Preview Page</Button>
            </Box>
            <ModalDialog open={openArticle} style={{ width: "600px" }} onClose={handleClose} aria-labelledby="modal-modal-title" aria-describedby="modal-modal-description" >
                <ArticleModal
                    keywordOptions={keywordOptions ?? []}
                    topicsOptions={topicsOptions ?? []}
                    uniqueID={uniqueID}
                    onSave={handleArticleUpdated}
                    articlesLoad={articlesLoad}
                    editArticle={editArticle}
                    imageList={imageList}
                />
            </ModalDialog>
            <ModalDialog open={openArticleSearch} childDirty={modalDirty} onArticleUpdated={handleArticleModalUpdated} onClose={handleArticleSearchClose} dialogStyle={dialogStyle} aria-labelledby="modal-modal-title" aria-describedby="modal-modal-description" >
                <SearchGrid
                    keywordOptions={keywordOptions ?? []}
                    topicsOptions={topicsOptions ?? []}
                    searchType="page"
                    onRowSelect={handleArticleRowClick}
                    onSearch={handleArticleSearch}
                    pageSize={pageSize}
                    columns={generateColumns(columns)}
                    items={tempArticles}
                    imageList={[]}
                />
            </ModalDialog>
            <ModalDialog open={previewOpen} onClose={() => setPreviewOpen(false)} style={{ width: '80%', maxWidth: 1200 }}>
                <Box sx={{ p: 2, display: 'flex', flexDirection: 'column', maxHeight: '80vh' }}>
                    <h3 style={{ marginTop: 0 }}>{previewTitle}</h3>
                    <Box sx={{ flex: 1, minHeight: 0, overflowY: 'auto' }}>
                        <HtmlContentRenderer htmlContent={previewHTML} />
                    </Box>
                </Box>
            </ModalDialog>
            <ModalDialog open={pagePreviewOpen} onClose={() => setPagePreviewOpen(false)} style={{ width: '85%', maxWidth: 1400 }}>
                <Box sx={{ p: 2, display: 'flex', flexDirection: 'column', maxHeight: '85vh' }}>
                    <h3 style={{ marginTop: 0 }}>{pagePreviewTitle}</h3>
                    <Box sx={{ flex: 1, minHeight: 0, overflowY: 'auto', display: 'flex', flexDirection: 'column', gap: 2 }}>
                        {pagePreviewItems.map(item => (
                            <Box key={item.id || item.name} sx={{ p: 1.5, border: '1px solid #e0e0e0', borderRadius: 1 }}>
                                <h4 style={{
                                    margin: '0 0 8px 0',
                                    padding: '6px 10px',
                                    background: '#f5f7fb',
                                    borderLeft: '4px solid #1976d2',
                                    fontSize: '1.05rem'
                                }}>
                                    Article: {item.name || 'Untitled Article'}
                                </h4>
                                <HtmlContentRenderer htmlContent={item.html} />
                            </Box>
                        ))}
                    </Box>
                </Box>
            </ModalDialog>
        </>
    );
};
export default CreateEditPage;

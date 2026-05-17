import React, { useEffect, useState, useCallback } from 'react';
import { useAppStateContext } from '../../hooks/appState/useAppStateContext';
import { SearchGrid } from '@tbirdcomponents/reactcomponents';
import './PageCollectionsEditor.css';
import {
    Box,
    Button,
    Card,
    CardContent,
    Typography,
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
    TextField,
    FormControl,
    InputLabel,
    Select,
    MenuItem,
    Chip,
    IconButton,
    List,
    ListItem,
    ListItemText,
    ListItemSecondaryAction,
    Divider,
    Alert,
    Snackbar,
    CircularProgress
} from '@mui/material';
import {
    Add as AddIcon,
    Edit as EditIcon,
    Delete as DeleteIcon,
    Save as SaveIcon,
    Cancel as CancelIcon,
    DragIndicator as DragIcon
} from '@mui/icons-material';

function PageCollectionsEditor({ config }) {
    const { DatabaseProcessing, WebSiteState } = useAppStateContext();
    
    // State management
    const [collections, setCollections] = useState([]);
    const [pages, setPages] = useState([]);
    const [selectedCollection, setSelectedCollection] = useState(null);
    const [editingCollection, setEditingCollection] = useState(null);
    const [isDialogOpen, setIsDialogOpen] = useState(false);
    const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
    const [loading, setLoading] = useState(false);
    const [message, setMessage] = useState({ show: false, text: '', severity: 'success' });
    
    const [keywordOptions, setKeywordOptions] = useState([]);
    const [topicOptions, setTopicOptions] = useState([]);
    const [draggedIndex, setDraggedIndex] = useState(null);
    const [hasOrderChanges, setHasOrderChanges] = useState(false);
    const [savingOrder, setSavingOrder] = useState(false);

    // Search criteria for collections and pages
    const [collectionSearchCriteria] = useState({});

    // Ensure pages are ordered by sequence for consistent initial rendering
    const sortPagesBySequence = useCallback((pages) => {
        const copy = Array.isArray(pages) ? [...pages] : [];
        copy.sort((a, b) => {
            const aSeq = a?.sequence ?? Number.MAX_SAFE_INTEGER;
            const bSeq = b?.sequence ?? Number.MAX_SAFE_INTEGER;
            if (aSeq !== bSeq) return aSeq - bSeq;
            const aName = a?.name || '';
            const bName = b?.name || '';
            return aName.localeCompare(bName);
        });
        return copy;
    }, []);

    // Load keywords and topics for search
    const loadKeywordsAndTopics = useCallback(async () => {
        try {
            const [keywords, topics] = await Promise.all([
                DatabaseProcessing.getKeywords(),
                DatabaseProcessing.getTopics()
            ]);
            setKeywordOptions(keywords || []);
            setTopicOptions(topics || []);
        } catch (error) {
            console.error('Failed to load keywords and topics:', error);
        }
    }, [DatabaseProcessing]);

    // Load collections
    const loadCollections = useCallback(async () => {
        setLoading(true);
        try {
            const searchCriteria = {
                ...collectionSearchCriteria,
                websiteId: WebSiteState.websiteID()
            };
            const collectionsData = await DatabaseProcessing.searchCollection(searchCriteria);
            setCollections(collectionsData || []);
        } catch (error) {
            console.error('Failed to load collections:', error);
            showMessage('Failed to load collections', 'error');
        } finally {
            setLoading(false);
        }
    }, [DatabaseProcessing, WebSiteState, collectionSearchCriteria]);

    // Load initial data
    useEffect(() => {
        loadKeywordsAndTopics();
        loadCollections();
    }, [loadKeywordsAndTopics, loadCollections]);

    // Search collections
    const handleCollectionSearch = async (criteria) => {
        setLoading(true);
        try {
            const searchCriteria = {
                ...criteria,
                websiteId: WebSiteState.websiteID()
            };
            const collectionsData = await DatabaseProcessing.searchCollection(searchCriteria);
            setCollections(collectionsData || []);
        } catch (error) {
            console.error('Collection search failed:', error);
            showMessage('Collection search failed', 'error');
        } finally {
            setLoading(false);
        }
    };

    // Search pages
    const handlePageSearch = async (criteria) => {
        setLoading(true);
        try {
            const searchCriteria = {
                ...criteria,
                websiteId: WebSiteState.websiteID()
            };
            const pagesData = await DatabaseProcessing.searchPage(searchCriteria);
            setPages(pagesData || []);
        } catch (error) {
            console.error('Page search failed:', error);
            showMessage('Page search failed', 'error');
        } finally {
            setLoading(false);
        }
    };

    // Show message
    const showMessage = (text, severity = 'success') => {
        setMessage({ show: true, text, severity });
    };

    // Handle collection selection
    const handleCollectionSelect = async (collection) => {
        if (!collection) {
            setSelectedCollection(null);
            return;
        }

        try {
            const collectionDetail = await DatabaseProcessing.getCollectionPage(collection.id);
            setSelectedCollection({
                ...collectionDetail,
                pages: sortPagesBySequence(collectionDetail?.pages)
            });
        } catch (error) {
            console.error('Failed to load collection details:', error);
            showMessage('Failed to load collection details', 'error');
        }
    };

    // Drag and drop handlers for pages list
    const handleDragStart = (index) => (event) => {
        setDraggedIndex(index);
        try {
            event.dataTransfer.effectAllowed = 'move';
            event.dataTransfer.setData('text/plain', String(index));
        } catch (e) {
            // noop for environments that restrict dataTransfer
        }
    };

    const handleDragOver = () => (event) => {
        event.preventDefault();
        try {
            event.dataTransfer.dropEffect = 'move';
        } catch (e) {
            // ignore
        }
    };

    const handleDrop = (index) => (event) => {
        event.preventDefault();
        if (!selectedCollection) return;
        let fromIndex = draggedIndex;
        if (fromIndex === null) {
            const data = event.dataTransfer.getData('text/plain');
            const parsed = parseInt(data, 10);
            fromIndex = Number.isNaN(parsed) ? null : parsed;
        }
        if (fromIndex === null || fromIndex === index) return;
        const newPages = Array.from(selectedCollection.pages);
        const [moved] = newPages.splice(fromIndex, 1);
        newPages.splice(index, 0, moved);
        setSelectedCollection({
            ...selectedCollection,
            pages: newPages
        });
        setHasOrderChanges(true);
        setDraggedIndex(null);
    };

    const handleSaveOrder = async () => {
        if (!selectedCollection) return;
        setSavingOrder(true);
        try {
            const reorderedPages = (selectedCollection.pages || []).map((p, idx) => ({
                ...p,
                sequence: idx + 1
            }));
            const payload = {
                collection: selectedCollection.collection,
                pages: reorderedPages
            };
            await DatabaseProcessing.AssociatePagesWithCollection(payload);
            const updated = await DatabaseProcessing.getCollectionPage(selectedCollection.collection.id);
            setSelectedCollection({
                ...updated,
                pages: sortPagesBySequence(updated?.pages)
            });
            setHasOrderChanges(false);
            showMessage('Order saved');
        } catch (error) {
            const status = error?.response?.status || error?.$metadata?.httpStatusCode;
            const data = error?.response?.data || error?.response?.body || error?.message;
            console.error('Failed to save order:', {
                status,
                url: `/collection/${selectedCollection?.collection?.id}/page`,
                payload: {
                    collection: selectedCollection?.collection,
                    pages: (selectedCollection?.pages || []).map((p, idx) => ({ id: p.id, sequence: idx + 1 }))
                },
                error
            });
            const display = typeof data === 'string' ? data : JSON.stringify(data);
            showMessage(`Failed to save order${status ? ` (HTTP ${status})` : ''}${display ? `: ${display}` : ''}`, 'error');
        } finally {
            setSavingOrder(false);
        }
    };

    // Create new collection
    const handleCreateCollection = () => {
        setEditingCollection({
            id: null,
            name: '',
            description: '',
            type: 'standard',
            websiteId: WebSiteState.websiteID()
        });
        setIsDialogOpen(true);
    };

    // Edit collection
    const handleEditCollection = (collection) => {
        setEditingCollection({ ...collection });
        setIsDialogOpen(true);
    };

    // Save collection
    const handleSaveCollection = async () => {
        if (!editingCollection.name.trim()) {
            showMessage('Collection name is required', 'error');
            return;
        }

        setLoading(true);
        try {
            await DatabaseProcessing.upsertCollections(editingCollection);
            showMessage(`Collection ${editingCollection.id ? 'updated' : 'created'} successfully`);
            setIsDialogOpen(false);
            setEditingCollection(null);
            loadCollections();
        } catch (error) {
            console.error('Failed to save collection:', error);
            showMessage('Failed to save collection', 'error');
        } finally {
            setLoading(false);
        }
    };

    // Delete collection
    const handleDeleteCollection = async () => {
        if (!selectedCollection) return;

        setLoading(true);
        try {
            await DatabaseProcessing.DeleteCollection(selectedCollection.collection.id);
            showMessage('Collection deleted successfully');
            setIsDeleteDialogOpen(false);
            setSelectedCollection(null);
            loadCollections();
        } catch (error) {
            console.error('Failed to delete collection:', error);
            showMessage('Failed to delete collection', 'error');
        } finally {
            setLoading(false);
        }
    };

    // Add page to collection
    const handleAddPageToCollection = async (page) => {
        if (!selectedCollection) return;

        try {
            const collectionPageDetail = {
                collection: selectedCollection.collection,
                pages: [{
                    ...page,
                    sequence: selectedCollection.pages.length + 1
                }]
            };

            await DatabaseProcessing.AssociatePagesWithCollection(collectionPageDetail);
            
            // Reload the collection details
            const updatedCollection = await DatabaseProcessing.getCollectionPage(selectedCollection.collection.id);
            setSelectedCollection({
                ...updatedCollection,
                pages: sortPagesBySequence(updatedCollection?.pages)
            });
            showMessage('Page added to collection');
        } catch (error) {
            console.error('Failed to add page to collection:', error);
            showMessage('Failed to add page to collection', 'error');
        }
    };

    // Remove page from collection
    const handleRemovePageFromCollection = async (pageId) => {
        if (!selectedCollection) return;

        try {
            await DatabaseProcessing.DeletePageFromCollection(selectedCollection.collection.id, pageId);
            const updatedPages = selectedCollection.pages.filter(page => page.id !== pageId);
            setSelectedCollection({
                ...selectedCollection,
                pages: updatedPages
            });
            setHasOrderChanges(true);
            showMessage('Page removed from collection');
        } catch (error) {
            console.error('Failed to remove page from collection:', error);
            showMessage('Failed to remove page from collection', 'error');
        }
    };

    // Collection search columns
    const collectionColumns = [
        { field: 'name', headerName: 'Name', width: 200 },
        { field: 'description', headerName: 'Description', width: 300 },
        { field: 'type', headerName: 'Type', width: 120 }
    ];

    // Page search columns
    const pageColumns = [
        { field: 'name', headerName: 'Name', width: 200 },
        { field: 'description', headerName: 'Description', width: 300 }
    ];

    return (
        <Box className="page-collections-editor">
            <Typography variant="h4" gutterBottom>
                Page Collections Editor
            </Typography>

            {/* Collections Section */}
            <Card sx={{ mb: 3 }}>
                <CardContent>
                    <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
                        <Typography variant="h6">Collections</Typography>
                        <Button
                            variant="contained"
                            startIcon={<AddIcon />}
                            onClick={handleCreateCollection}
                        >
                            Create Collection
                        </Button>
                    </Box>

                                         <SearchGrid
                         columns={collectionColumns}
                         items={collections}
                         onSearch={handleCollectionSearch}
                         onRowSelect={handleCollectionSelect}
                         searchType="collection"
                         keywordOptions={keywordOptions}
                         topicsOptions={topicOptions}
                         pageSizeOptions={[10, 25, 50, 100]}
                         pageSize={25}
                     />
                </CardContent>
            </Card>

            {/* Selected Collection Details */}
            {selectedCollection && (
                <Card sx={{ mb: 3 }}>
                    <CardContent>
                        <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
                            <Typography variant="h6">
                                Collection: {selectedCollection.collection.name}
                            </Typography>
                            <Box>
                                <IconButton
                                    onClick={() => handleEditCollection(selectedCollection.collection)}
                                    color="primary"
                                >
                                    <EditIcon />
                                </IconButton>
                                <IconButton
                                    onClick={() => setIsDeleteDialogOpen(true)}
                                    color="error"
                                >
                                    <DeleteIcon />
                                </IconButton>
                            </Box>
                        </Box>

                        <Typography variant="body2" color="textSecondary" gutterBottom>
                            {selectedCollection.collection.description}
                        </Typography>

                        <Chip 
                            label={selectedCollection.collection.type} 
                            color={selectedCollection.collection.type === 'gallery' ? 'primary' : 'default'}
                            sx={{ mb: 2 }}
                        />

                        <Box display="flex" justifyContent="space-between" alignItems="center" mb={1}>
                            <Typography variant="h6" gutterBottom>
                                Pages in Collection ({selectedCollection.pages.length})
                            </Typography>
                            <Button
                                variant="contained"
                                startIcon={<SaveIcon />}
                                onClick={handleSaveOrder}
                                disabled={!hasOrderChanges || savingOrder}
                            >
                                {savingOrder ? <CircularProgress size={20} /> : 'Save Order'}
                            </Button>
                        </Box>

                        <List>
                            {selectedCollection.pages.map((page, index) => (
                                <React.Fragment key={page.id}>
                                    <ListItem
                                        draggable
                                        onDragStart={handleDragStart(index)}
                                        onDragOver={handleDragOver(index)}
                                        onDrop={handleDrop(index)}
                                        sx={{ cursor: 'grab' }}
                                    >
                                        <DragIcon sx={{ mr: 1, color: 'text.secondary' }} />
                                        <ListItemText
                                            primary={page.name}
                                            secondary={page.description}
                                        />
                                        <ListItemSecondaryAction>
                                            <IconButton
                                                onClick={() => handleRemovePageFromCollection(page.id)}
                                                color="error"
                                                size="small"
                                            >
                                                <DeleteIcon />
                                            </IconButton>
                                        </ListItemSecondaryAction>
                                    </ListItem>
                                    {index < selectedCollection.pages.length - 1 && <Divider />}
                                </React.Fragment>
                            ))}
                        </List>
                    </CardContent>
                </Card>
            )}

            {/* Available Pages Section */}
            {selectedCollection && (
                <Card>
                    <CardContent>
                        <Typography variant="h6" gutterBottom>
                            Available Pages
                        </Typography>
                        <Typography variant="body2" color="textSecondary" gutterBottom>
                            Search and add pages to the collection
                        </Typography>

                                                 <SearchGrid
                             columns={pageColumns}
                             items={pages}
                             onSearch={handlePageSearch}
                             onRowSelect={handleAddPageToCollection}
                             searchType="page"
                             keywordOptions={keywordOptions}
                             topicsOptions={topicOptions}
                             pageSizeOptions={[10, 25, 50, 100]}
                             pageSize={25}
                         />
                    </CardContent>
                </Card>
            )}

            {/* Collection Edit Dialog */}
            <Dialog open={isDialogOpen} onClose={() => setIsDialogOpen(false)} maxWidth="sm" fullWidth>
                <DialogTitle>
                    {editingCollection?.id ? 'Edit Collection' : 'Create Collection'}
                </DialogTitle>
                <DialogContent>
                    <TextField
                        fullWidth
                        label="Name"
                        value={editingCollection?.name || ''}
                        onChange={(e) => setEditingCollection({
                            ...editingCollection,
                            name: e.target.value
                        })}
                        margin="normal"
                        required
                    />
                    <TextField
                        fullWidth
                        label="Description"
                        value={editingCollection?.description || ''}
                        onChange={(e) => setEditingCollection({
                            ...editingCollection,
                            description: e.target.value
                        })}
                        margin="normal"
                        multiline
                        rows={3}
                    />
                    <FormControl fullWidth margin="normal">
                        <InputLabel>Type</InputLabel>
                        <Select
                            value={editingCollection?.type || 'standard'}
                            onChange={(e) => setEditingCollection({
                                ...editingCollection,
                                type: e.target.value
                            })}
                            label="Type"
                        >
                            <MenuItem value="standard">Standard Collection</MenuItem>
                            <MenuItem value="gallery">Gallery Collection</MenuItem>
                        </Select>
                    </FormControl>
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setIsDialogOpen(false)}>
                        <CancelIcon /> Cancel
                    </Button>
                    <Button 
                        onClick={handleSaveCollection} 
                        variant="contained"
                        disabled={loading}
                    >
                        {loading ? <CircularProgress size={20} /> : <SaveIcon />}
                        Save
                    </Button>
                </DialogActions>
            </Dialog>

            {/* Delete Confirmation Dialog */}
            <Dialog open={isDeleteDialogOpen} onClose={() => setIsDeleteDialogOpen(false)}>
                <DialogTitle>Delete Collection</DialogTitle>
                <DialogContent>
                    <Typography>
                        Are you sure you want to delete "{selectedCollection?.collection.name}"? 
                        This action cannot be undone.
                    </Typography>
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setIsDeleteDialogOpen(false)}>
                        Cancel
                    </Button>
                    <Button 
                        onClick={handleDeleteCollection} 
                        color="error" 
                        variant="contained"
                        disabled={loading}
                    >
                        {loading ? <CircularProgress size={20} /> : <DeleteIcon />}
                        Delete
                    </Button>
                </DialogActions>
            </Dialog>

            {/* Message Snackbar */}
            <Snackbar
                open={message.show}
                autoHideDuration={6000}
                onClose={() => setMessage({ ...message, show: false })}
            >
                <Alert 
                    onClose={() => setMessage({ ...message, show: false })} 
                    severity={message.severity}
                >
                    {message.text}
                </Alert>
            </Snackbar>
        </Box>
    );
}

export default PageCollectionsEditor;
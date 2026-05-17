import React, { useEffect, useState, useCallback, useMemo } from 'react';
import { useNavigate, useParams, useSearchParams, useOutletContext } from 'react-router-dom';
import {
    Box,
    Button,
    Card,
    CardContent,
    Typography,
    Chip,
    IconButton,
    TextField,
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
    Snackbar,
    Alert,
    CircularProgress,
    Stack,
    Tooltip
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import PublishIcon from '@mui/icons-material/Publish';
import UnpublishedIcon from '@mui/icons-material/Unpublished';
import EditIcon from '@mui/icons-material/Edit';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { SearchGrid } from '@tbirdcomponents/reactcomponents';
import { useAppStateContext } from '../../hooks/appState/useAppStateContext';
import PageEditor from '../../sitecontent/contentAdmin/PageEditor';

function StatusChip({ status }) {
    const value = status || 'published';
    return (
        <Chip
            size="small"
            label={value}
            color={value === 'published' ? 'success' : 'warning'}
        />
    );
}

function PagesManager() {
    const navigate = useNavigate();
    const params = useParams();
    const [searchParams, setSearchParams] = useSearchParams();
    const { config } = useOutletContext() ?? {};
    const { DatabaseProcessing, WebSiteState } = useAppStateContext();

    const [pages, setPages] = useState([]);
    const [loading, setLoading] = useState(false);
    const [keywordOptions, setKeywordOptions] = useState([]);
    const [topicOptions, setTopicOptions] = useState([]);
    const imageList = [];
    const [newPageDialogOpen, setNewPageDialogOpen] = useState(false);
    const [newPageName, setNewPageName] = useState('');
    const [creating, setCreating] = useState(false);
    const [message, setMessage] = useState({ open: false, text: '', severity: 'success' });
    const [editingPageId, setEditingPageId] = useState(null);

    const showMessage = useCallback((text, severity = 'success') => {
        setMessage({ open: true, text, severity });
    }, []);

    const loadMetadata = useCallback(async () => {
        try {
            const [keywords, topics] = await Promise.all([
                DatabaseProcessing.getKeywords(),
                DatabaseProcessing.getTopics()
            ]);
            setKeywordOptions(keywords || []);
            setTopicOptions(topics || []);
        } catch (err) {
            console.error('PagesManager: failed to load keywords/topics', err);
        }
    }, [DatabaseProcessing]);

    const loadPages = useCallback(async (criteria = {}) => {
        setLoading(true);
        try {
            const websiteId = WebSiteState.websiteID();
            const result = await DatabaseProcessing.searchPage({
                ...criteria,
                WebsiteId: websiteId,
                websiteId
            });
            setPages(Array.isArray(result) ? result : []);
        } catch (err) {
            console.error('PagesManager: search failed', err);
            showMessage('Failed to load pages', 'error');
        } finally {
            setLoading(false);
        }
    }, [DatabaseProcessing, WebSiteState, showMessage]);

    useEffect(() => { loadMetadata(); loadPages(); }, [loadMetadata, loadPages]);

    useEffect(() => {
        if (params.id) {
            setEditingPageId(params.id);
        } else if (searchParams.get('new') === '1') {
            setNewPageDialogOpen(true);
            const next = new URLSearchParams(searchParams);
            next.delete('new');
            setSearchParams(next, { replace: true });
        } else {
            setEditingPageId(null);
        }
    }, [params.id, searchParams, setSearchParams]);

    const handleNewPage = useCallback(async () => {
        const name = newPageName.trim();
        if (!name) {
            showMessage('Page name is required', 'error');
            return;
        }
        setCreating(true);
        try {
            const websiteId = WebSiteState.websiteID();
            const newPage = await DatabaseProcessing.createPageWithDefaultArticle(name, websiteId, config);
            setNewPageDialogOpen(false);
            setNewPageName('');
            showMessage(`Created page "${name}"`);
            if (newPage?.id) {
                navigate(`/admin/pages/${newPage.id}`);
            } else {
                loadPages();
            }
        } catch (err) {
            console.error('PagesManager: create page failed', err);
            showMessage('Failed to create page', 'error');
        } finally {
            setCreating(false);
        }
    }, [newPageName, DatabaseProcessing, WebSiteState, config, navigate, loadPages, showMessage]);

    const handlePublishToggle = useCallback(async (page) => {
        const isPublished = (page.status || 'published') === 'published';
        try {
            if (isPublished) {
                await DatabaseProcessing.unpublishPage(page.id);
                showMessage(`Unpublished "${page.name}"`);
            } else {
                await DatabaseProcessing.publishPage(page.id);
                showMessage(`Published "${page.name}"`);
            }
            loadPages();
        } catch (err) {
            console.error('PagesManager: publish toggle failed', err);
            showMessage('Publish action failed (requires Track 2 deploy)', 'error');
        }
    }, [DatabaseProcessing, loadPages, showMessage]);

    const columns = useMemo(() => [
        { field: 'name', headerName: 'Name', width: 220 },
        { field: 'layout', headerName: 'Layout', width: 150 },
        {
            field: 'status',
            headerName: 'Status',
            width: 130,
            renderCell: (params) => <StatusChip status={params.row.status} />
        },
        {
            field: 'actions',
            headerName: 'Actions',
            width: 180,
            sortable: false,
            renderCell: (params) => {
                const row = params.row;
                const isPublished = (row.status || 'published') === 'published';
                return (
                    <Stack direction="row" spacing={0.5}>
                        <Tooltip title="Edit">
                            <IconButton size="small" onClick={(e) => { e.stopPropagation(); navigate(`/admin/pages/${row.id}`); }}>
                                <EditIcon fontSize="small" />
                            </IconButton>
                        </Tooltip>
                        <Tooltip title={isPublished ? 'Unpublish' : 'Publish'}>
                            <IconButton
                                size="small"
                                onClick={(e) => { e.stopPropagation(); handlePublishToggle(row); }}
                                color={isPublished ? 'warning' : 'success'}
                            >
                                {isPublished ? <UnpublishedIcon fontSize="small" /> : <PublishIcon fontSize="small" />}
                            </IconButton>
                        </Tooltip>
                    </Stack>
                );
            }
        }
    ], [navigate, handlePublishToggle]);

    const handleSavePage = useCallback((page) => {
        showMessage(`Saved "${page?.name ?? 'page'}"`);
        loadPages();
    }, [loadPages, showMessage]);

    if (editingPageId) {
        return (
            <Box>
                <Button startIcon={<ArrowBackIcon />} onClick={() => navigate('/admin/pages')} sx={{ mb: 2 }}>
                    Back to pages
                </Button>
                <PageEditor
                    config={config}
                    imageList={imageList}
                    onSavePage={handleSavePage}
                    createNewPage={false}
                />
            </Box>
        );
    }

    return (
        <Box>
            <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
                <Typography variant="h4">Pages</Typography>
                <Button
                    variant="contained"
                    startIcon={<AddIcon />}
                    onClick={() => setNewPageDialogOpen(true)}
                >
                    New Page
                </Button>
            </Box>

            <Card>
                <CardContent>
                    <SearchGrid
                        columns={columns}
                        items={pages}
                        onSearch={loadPages}
                        onRowSelect={(row) => row?.id && navigate(`/admin/pages/${row.id}`)}
                        searchType="page"
                        keywordOptions={keywordOptions}
                        topicsOptions={topicOptions}
                        pageSizeOptions={[10, 25, 50, 100]}
                        pageSize={25}
                        imageList={imageList}
                    />
                    {loading && (
                        <Box display="flex" justifyContent="center" mt={2}>
                            <CircularProgress size={24} />
                        </Box>
                    )}
                </CardContent>
            </Card>

            <Dialog open={newPageDialogOpen} onClose={() => setNewPageDialogOpen(false)} maxWidth="sm" fullWidth>
                <DialogTitle>New Page</DialogTitle>
                <DialogContent>
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        A default article will be auto-created so you can start writing immediately.
                    </Typography>
                    <TextField
                        autoFocus
                        fullWidth
                        label="Page Name"
                        value={newPageName}
                        onChange={(e) => setNewPageName(e.target.value)}
                        onKeyDown={(e) => { if (e.key === 'Enter') handleNewPage(); }}
                        margin="normal"
                    />
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setNewPageDialogOpen(false)} disabled={creating}>Cancel</Button>
                    <Button variant="contained" onClick={handleNewPage} disabled={creating || !newPageName.trim()}>
                        {creating ? <CircularProgress size={20} /> : 'Create & Edit'}
                    </Button>
                </DialogActions>
            </Dialog>

            <Snackbar
                open={message.open}
                autoHideDuration={4000}
                onClose={() => setMessage((m) => ({ ...m, open: false }))}
            >
                <Alert severity={message.severity} onClose={() => setMessage((m) => ({ ...m, open: false }))}>
                    {message.text}
                </Alert>
            </Snackbar>
        </Box>
    );
}

export default PagesManager;

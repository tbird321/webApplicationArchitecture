import React, { useEffect, useState, useCallback, useMemo } from 'react';
import { useOutletContext } from 'react-router-dom';
import {
    Box,
    Button,
    Card,
    CardContent,
    Typography,
    Chip,
    IconButton,
    Snackbar,
    Alert,
    CircularProgress,
    Stack,
    Tooltip,
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions
} from '@mui/material';
import PublishIcon from '@mui/icons-material/Publish';
import UnpublishedIcon from '@mui/icons-material/Unpublished';
import EditIcon from '@mui/icons-material/Edit';
import { SearchGrid, HtmlEditor } from '@tbirdcomponents/reactcomponents';
import { useAppStateContext } from '../../hooks/appState/useAppStateContext';

function StatusChip({ status }) {
    const value = status || 'published';
    return <Chip size="small" label={value} color={value === 'published' ? 'success' : 'warning'} />;
}

function ArticlesManager() {
    const { config } = useOutletContext() ?? {};
    const { DatabaseProcessing, FileProcessing, WebSiteState } = useAppStateContext();

    const [articles, setArticles] = useState([]);
    const [loading, setLoading] = useState(false);
    const [keywordOptions, setKeywordOptions] = useState([]);
    const [topicOptions, setTopicOptions] = useState([]);
    const [message, setMessage] = useState({ open: false, text: '', severity: 'success' });
    const [editingArticle, setEditingArticle] = useState(null);
    const [editingHtml, setEditingHtml] = useState('');
    const [loadingArticle, setLoadingArticle] = useState(false);

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
            console.error('ArticlesManager: failed to load metadata', err);
        }
    }, [DatabaseProcessing]);

    const loadArticles = useCallback(async (criteria = {}) => {
        setLoading(true);
        try {
            const websiteId = WebSiteState.websiteID();
            const result = await DatabaseProcessing.searchArticle({
                ...criteria,
                WebsiteId: websiteId,
                websiteId
            });
            setArticles(Array.isArray(result) ? result : []);
        } catch (err) {
            console.error('ArticlesManager: search failed', err);
            showMessage('Failed to load articles', 'error');
        } finally {
            setLoading(false);
        }
    }, [DatabaseProcessing, WebSiteState, showMessage]);

    useEffect(() => { loadMetadata(); loadArticles(); }, [loadMetadata, loadArticles]);

    const handleOpenArticle = useCallback(async (article) => {
        setLoadingArticle(true);
        setEditingArticle(article);
        try {
            const rootPath = `websites/${config.Site.siteName}/articles`;
            const html = await FileProcessing.getFileData(rootPath, article.articlePath);
            setEditingHtml(typeof html === 'string' ? html : '');
        } catch (err) {
            console.error('ArticlesManager: failed to load HTML', err);
            setEditingHtml('');
            showMessage('Failed to load article content', 'error');
        } finally {
            setLoadingArticle(false);
        }
    }, [FileProcessing, config, showMessage]);

    const handleSaveArticle = useCallback(async (html) => {
        if (!editingArticle) return;
        try {
            const rootPath = `websites/${config.Site.siteName}/articles`;
            await FileProcessing.saveFileData(rootPath, editingArticle.articlePath, html, 'text/html');
            setEditingHtml(html);
            showMessage('Article saved');
        } catch (err) {
            console.error('ArticlesManager: save failed', err);
            showMessage('Failed to save article', 'error');
        }
    }, [editingArticle, FileProcessing, config, showMessage]);

    const handlePublishToggle = useCallback(async (article) => {
        const isPublished = (article.status || 'published') === 'published';
        try {
            if (isPublished) {
                await DatabaseProcessing.unpublishArticle(article.id);
                showMessage(`Unpublished "${article.name}"`);
            } else {
                await DatabaseProcessing.publishArticle(article.id);
                showMessage(`Published "${article.name}"`);
            }
            loadArticles();
        } catch (err) {
            console.error('ArticlesManager: publish toggle failed', err);
            showMessage('Publish action failed (requires Track 2 deploy)', 'error');
        }
    }, [DatabaseProcessing, loadArticles, showMessage]);

    const columns = useMemo(() => [
        { field: 'name', headerName: 'Name', width: 220 },
        { field: 'articlePath', headerName: 'Path', width: 250 },
        {
            field: 'status',
            headerName: 'Status',
            width: 130,
            renderCell: (params) => <StatusChip status={params.row.status} />
        },
        {
            field: 'actions',
            headerName: 'Actions',
            width: 160,
            sortable: false,
            renderCell: (params) => {
                const row = params.row;
                const isPublished = (row.status || 'published') === 'published';
                return (
                    <Stack direction="row" spacing={0.5}>
                        <Tooltip title="Edit">
                            <IconButton size="small" onClick={(e) => { e.stopPropagation(); handleOpenArticle(row); }}>
                                <EditIcon fontSize="small" />
                            </IconButton>
                        </Tooltip>
                        <Tooltip title={isPublished ? 'Unpublish' : 'Publish'}>
                            <IconButton
                                size="small"
                                color={isPublished ? 'warning' : 'success'}
                                onClick={(e) => { e.stopPropagation(); handlePublishToggle(row); }}
                            >
                                {isPublished ? <UnpublishedIcon fontSize="small" /> : <PublishIcon fontSize="small" />}
                            </IconButton>
                        </Tooltip>
                    </Stack>
                );
            }
        }
    ], [handleOpenArticle, handlePublishToggle]);

    return (
        <Box>
            <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
                <Typography variant="h4">Articles</Typography>
            </Box>

            <Card>
                <CardContent>
                    <SearchGrid
                        columns={columns}
                        items={articles}
                        onSearch={loadArticles}
                        onRowSelect={handleOpenArticle}
                        searchType="article"
                        keywordOptions={keywordOptions}
                        topicsOptions={topicOptions}
                        pageSizeOptions={[10, 25, 50, 100]}
                        pageSize={25}
                    />
                    {loading && (
                        <Box display="flex" justifyContent="center" mt={2}>
                            <CircularProgress size={24} />
                        </Box>
                    )}
                </CardContent>
            </Card>

            <Dialog
                open={!!editingArticle}
                onClose={() => setEditingArticle(null)}
                maxWidth="lg"
                fullWidth
            >
                <DialogTitle>
                    Edit Article: {editingArticle?.name}
                </DialogTitle>
                <DialogContent>
                    {loadingArticle ? (
                        <Box display="flex" justifyContent="center" p={4}>
                            <CircularProgress />
                        </Box>
                    ) : (
                        <HtmlEditor
                            initialHtml={editingHtml}
                            onSave={handleSaveArticle}
                        />
                    )}
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setEditingArticle(null)}>Close</Button>
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

export default ArticlesManager;

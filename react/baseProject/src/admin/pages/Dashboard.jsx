import React, { useEffect, useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import {
    Box,
    Grid,
    Card,
    CardContent,
    CardHeader,
    Typography,
    List,
    ListItem,
    ListItemText,
    ListItemButton,
    Button,
    Chip,
    Divider,
    CircularProgress,
    Stack
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import MenuIcon from '@mui/icons-material/Menu';
import ArticleIcon from '@mui/icons-material/Article';
import { useAppStateContext } from '../../hooks/appState/useAppStateContext';

function Dashboard() {
    const navigate = useNavigate();
    const { DatabaseProcessing, WebSiteState } = useAppStateContext();

    const [recentPages, setRecentPages] = useState([]);
    const [loading, setLoading] = useState(true);
    const [statusSummary, setStatusSummary] = useState({ published: 0, draft: 0 });

    const loadRecent = useCallback(async () => {
        setLoading(true);
        try {
            const websiteId = WebSiteState.websiteID();
            const pages = await DatabaseProcessing.searchPage({ websiteId, WebsiteId: websiteId });
            const list = Array.isArray(pages) ? pages : [];
            setRecentPages(list.slice(0, 5));
            const published = list.filter((p) => (p.status || 'published') === 'published').length;
            const draft = list.filter((p) => p.status === 'draft').length;
            setStatusSummary({ published, draft });
        } catch (err) {
            console.error('Dashboard: failed to load recent pages', err);
            setRecentPages([]);
        } finally {
            setLoading(false);
        }
    }, [DatabaseProcessing, WebSiteState]);

    useEffect(() => { loadRecent(); }, [loadRecent]);

    return (
        <Box>
            <Typography variant="h4" gutterBottom>Dashboard</Typography>

            <Grid container spacing={3}>
                <Grid item xs={12} md={4}>
                    <Card>
                        <CardHeader title="Quick Actions" />
                        <CardContent>
                            <Stack spacing={1}>
                                <Button
                                    variant="contained"
                                    startIcon={<AddIcon />}
                                    onClick={() => navigate('/admin/pages?new=1')}
                                    fullWidth
                                >
                                    New Page
                                </Button>
                                <Button
                                    variant="outlined"
                                    startIcon={<MenuIcon />}
                                    onClick={() => navigate('/admin/menus')}
                                    fullWidth
                                >
                                    Edit Menu
                                </Button>
                                <Button
                                    variant="outlined"
                                    startIcon={<ArticleIcon />}
                                    onClick={() => navigate('/admin/articles')}
                                    fullWidth
                                >
                                    Manage Articles
                                </Button>
                            </Stack>
                        </CardContent>
                    </Card>
                </Grid>

                <Grid item xs={12} md={4}>
                    <Card>
                        <CardHeader title="Content Status" />
                        <CardContent>
                            <Stack direction="row" spacing={2}>
                                <Chip label={`Published: ${statusSummary.published}`} color="success" />
                                <Chip label={`Draft: ${statusSummary.draft}`} color="warning" />
                            </Stack>
                            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1 }}>
                                Counts reflect the `status` column once the draft/publish migration is live.
                            </Typography>
                        </CardContent>
                    </Card>
                </Grid>

                <Grid item xs={12} md={4}>
                    <Card>
                        <CardHeader title="Recent Pages" />
                        <CardContent>
                            {loading ? (
                                <Box sx={{ display: 'flex', justifyContent: 'center' }}>
                                    <CircularProgress size={24} />
                                </Box>
                            ) : recentPages.length === 0 ? (
                                <Typography variant="body2" color="text.secondary">
                                    No pages yet. Create your first page to get started.
                                </Typography>
                            ) : (
                                <List dense>
                                    {recentPages.map((p, idx) => (
                                        <React.Fragment key={p.id ?? idx}>
                                            <ListItem disablePadding>
                                                <ListItemButton onClick={() => navigate(`/admin/pages/${p.id}`)}>
                                                    <ListItemText
                                                        primary={p.name}
                                                        secondary={
                                                            <Chip
                                                                size="small"
                                                                label={p.status || 'published'}
                                                                color={(p.status || 'published') === 'published' ? 'success' : 'warning'}
                                                            />
                                                        }
                                                    />
                                                </ListItemButton>
                                            </ListItem>
                                            {idx < recentPages.length - 1 && <Divider />}
                                        </React.Fragment>
                                    ))}
                                </List>
                            )}
                        </CardContent>
                    </Card>
                </Grid>
            </Grid>
        </Box>
    );
}

export default Dashboard;

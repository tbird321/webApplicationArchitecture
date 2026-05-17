import React, { useState, useEffect, useCallback, useMemo, useRef } from 'react';
import { useAppStateContext } from '../../hooks/appState/useAppStateContext';
import { HtmlContentRenderer } from '@tbirdcomponents/reactcomponents';
import ArticleEditor from './ArticleEditor';
import { HtmlEditor } from '@tbirdcomponents/reactcomponents';
import { IconButton, Box, Typography, Paper, Chip } from '@mui/material';
import { ChevronLeft, ChevronRight, Edit, Close } from '@mui/icons-material';
import './PageGallery.css';

const PageGallery = ({ 
    pages, 
    config, 
    imageList, 
    onPageChange, 
    onPageEdit,
    editMode = false,
    showHeader = true,
    showHeaderContent = false,
}) => {
    const { FileProcessing, WebSiteState, DatabaseProcessing } = useAppStateContext();
    const [currentPageIndex, setCurrentPageIndex] = useState(0);
    const [pageContents, setPageContents] = useState({});
    const pageContentsRef = useRef({});
    const prefetchingIdsRef = useRef(new Set());
    const [loadingStates, setLoadingStates] = useState({});
    const [editPageIndex, setEditPageIndex] = useState(null);
    const [dynamicCSSURL, setDynamicCSSURL] = useState('');
    const touchStartXRef = useRef(null);
    const touchStartYRef = useRef(null);
    const mouseDownRef = useRef(false);
    const mouseStartXRef = useRef(null);
    const mouseStartYRef = useRef(null);

    // Get current page data
    const currentPage = useMemo(() => {
        return pages && pages.length > 0 ? pages[currentPageIndex] : null;
    }, [pages, currentPageIndex]);

    // Debug: log pages passed into gallery (controlled by config.Site.appLoggingEnabled)
    useEffect(() => {
        if (config?.Site?.appLoggingEnabled) {
            // eslint-disable-next-line no-console
            console.log('PageGallery pages:', pages);
        }
    }, [pages, config?.Site?.appLoggingEnabled]);

    // Debug: log articles for the current page when available (controlled by config.Site.appLoggingEnabled)
    useEffect(() => {
        if (!currentPage || !config?.Site?.appLoggingEnabled) return;
        const current = pageContents[currentPage.id];
        if (current && Array.isArray(current.articles)) {
            // eslint-disable-next-line no-console
            console.log('PageGallery articles for page', currentPage.id, current.articles);
        }
    }, [currentPage, pageContents, config?.Site?.appLoggingEnabled]);

    // Keep ref in sync to avoid capturing state in effects
    useEffect(() => {
        pageContentsRef.current = pageContents;
    }, [pageContents]);

    // Helper: fetch and store a page's content (used for current and prefetch)
    const fetchAndStorePageContent = useCallback(async (page, showLoading = true) => {
        if (!page || pageContentsRef.current[page.id]) return;
        if (showLoading) {
            setLoadingStates(prev => ({ ...prev, [page.id]: true }));
        }
        try {
            const rootPath = `websites/${config.Site.siteName}`;
            let headerHTML = '';
            let articlesWithHTML = [];

            if (showHeaderContent && await FileProcessing.fileExists(rootPath, config.Site.headerFileName)) {
                headerHTML = await FileProcessing.getFileData(rootPath, config.Site.headerFileName);
            }

            const websiteId = config?.Site?.websiteId ?? WebSiteState.websiteID();
            if (page.id) {
                const fullPage = await DatabaseProcessing.getPageById(page.id, websiteId);
                if (fullPage?.articles && fullPage.articles.length > 0) {
                    const isHeaderArticle = (art) => {
                        const name = (art?.name || '').toLowerCase();
                        const isFirstSequence = typeof art?.sequence_no === 'number' && art.sequence_no === 1;
                        return isFirstSequence || name.includes('header') || name.includes('page header');
                    };
                    const contentArticles = fullPage.articles.filter(a => !isHeaderArticle(a));
                    articlesWithHTML = await Promise.all(
                        contentArticles.map(async (article) => {
                            try {
                                if (article.articlePath) {
                                    const articleHTML = await FileProcessing.getFileData(
                                        `${rootPath}/articles`,
                                        article.articlePath
                                    );
                                    return { ...article, articleHTML };
                                }
                            } catch (error) {
                                console.error("Failed to fetch article HTML:", error);
                                return { ...article, articleHTML: '' };
                            }
                            return { ...article, articleHTML: '' };
                        })
                    );
                }
            }

            setPageContents(prev => ({
                ...prev,
                [page.id]: {
                    headerHTML,
                    articles: articlesWithHTML
                }
            }));
        } catch (error) {
            console.error("Failed to load page content:", error);
        } finally {
            if (showLoading) {
                setLoadingStates(prev => ({ ...prev, [page.id]: false }));
            }
        }
    }, [config.Site.headerFileName, config.Site.siteName, config?.Site?.websiteId, FileProcessing, WebSiteState, DatabaseProcessing, showHeaderContent]);

    // Load current page on demand
    useEffect(() => {
        if (!currentPage || pageContentsRef.current[currentPage.id]) return;
        fetchAndStorePageContent(currentPage, true);
    }, [currentPage, fetchAndStorePageContent]);

    // Prefetch neighbor pages to reduce perceived latency
    useEffect(() => {
        if (!pages || pages.length === 0) return;
        const neighborIndices = [currentPageIndex + 1, currentPageIndex - 1, currentPageIndex + 2];
        neighborIndices.forEach((idx) => {
            if (idx < 0 || idx >= pages.length) return;
            const neighbor = pages[idx];
            if (!neighbor || pageContentsRef.current[neighbor.id] || prefetchingIdsRef.current.has(neighbor.id)) return;
            prefetchingIdsRef.current.add(neighbor.id);
            fetchAndStorePageContent(neighbor, false).finally(() => {
                prefetchingIdsRef.current.delete(neighbor.id);
            });
        });
    }, [currentPageIndex, pages, fetchAndStorePageContent]);

    // Set up dynamic CSS loading
    useEffect(() => {
        const setCSSFileURL = () => {
            const rootPath = `${config.Site.appURL}public/assets/${config.Site.siteName}/themes`;
            const cssURL = `${rootPath}/${config.Site.themeFileName}`;
            if (config.Site.themeFileName) {
                setDynamicCSSURL(cssURL);
            }
        };

        setCSSFileURL();
    }, [config]);

    // Dynamic CSS injection
    useEffect(() => {
        if (!dynamicCSSURL) return;

        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = dynamicCSSURL;
        document.head.appendChild(link);

        return () => {
            if (document.head.contains(link)) {
                document.head.removeChild(link);
            }
        };
    }, [dynamicCSSURL]);

    // Navigation handlers
    const handlePreviousPage = useCallback(() => {
        if (currentPageIndex > 0) {
            setCurrentPageIndex(prev => prev - 1);
        }
    }, [currentPageIndex]);

    const handleNextPage = useCallback(() => {
        if (currentPageIndex < pages.length - 1) {
            setCurrentPageIndex(prev => prev + 1);
        }
    }, [currentPageIndex, pages.length]);

    // Touch swipe handlers for mobile navigation
    const SWIPE_THRESHOLD_PX = 40;
    const handleTouchStart = useCallback((event) => {
        const touch = event.touches && event.touches[0];
        if (!touch) return;
        touchStartXRef.current = touch.clientX;
        touchStartYRef.current = touch.clientY;
    }, []);

    const handleTouchEnd = useCallback((event) => {
        const touch = event.changedTouches && event.changedTouches[0];
        if (!touch) return;
        const startX = touchStartXRef.current;
        const startY = touchStartYRef.current;
        if (startX == null || startY == null) return;

        const deltaX = touch.clientX - startX;
        const deltaY = touch.clientY - startY;
        // Ensure primarily horizontal swipe
        if (Math.abs(deltaX) > SWIPE_THRESHOLD_PX && Math.abs(deltaX) > Math.abs(deltaY)) {
            if (deltaX > 0) {
                handlePreviousPage();
            } else {
                handleNextPage();
            }
        }
        touchStartXRef.current = null;
        touchStartYRef.current = null;
    }, [handleNextPage, handlePreviousPage]);

    // Mouse drag handlers for desktop navigation
    const handleMouseDown = useCallback((event) => {
        mouseDownRef.current = true;
        mouseStartXRef.current = event.clientX;
        mouseStartYRef.current = event.clientY;
    }, []);

    const handleMouseMove = useCallback((event) => {
        if (!mouseDownRef.current) return;
        // prevent text selection when dragging
        event.preventDefault();
    }, []);

    const endMouseDrag = useCallback((event) => {
        if (!mouseDownRef.current) return;
        mouseDownRef.current = false;
        const startX = mouseStartXRef.current;
        const startY = mouseStartYRef.current;
        mouseStartXRef.current = null;
        mouseStartYRef.current = null;
        if (startX == null || startY == null) return;
        const deltaX = event.clientX - startX;
        const deltaY = event.clientY - startY;
        if (Math.abs(deltaX) > SWIPE_THRESHOLD_PX && Math.abs(deltaX) > Math.abs(deltaY)) {
            if (deltaX > 0) {
                handlePreviousPage();
            } else {
                handleNextPage();
            }
        }
    }, [handleNextPage, handlePreviousPage]);

    // Keyboard navigation
    useEffect(() => {
        const handleKeyPress = (event) => {
            if (event.key === 'ArrowLeft') {
                handlePreviousPage();
            } else if (event.key === 'ArrowRight') {
                handleNextPage();
            } else if (event.key === 'Escape' && editPageIndex !== null) {
                setEditPageIndex(null);
            }
        };

        window.addEventListener('keydown', handleKeyPress);
        return () => window.removeEventListener('keydown', handleKeyPress);
    }, [handlePreviousPage, handleNextPage, editPageIndex]);

    // Handle page edit
    const handlePageEdit = useCallback((pageIndex) => {
        setEditPageIndex(pageIndex);
        if (onPageEdit) {
            onPageEdit(pages[pageIndex]);
        }
    }, [pages, onPageEdit]);

    // Handle page save
    const handlePageSave = useCallback(async (updatedContent) => {
        try {
            const rootPath = `websites/${config.Site.siteName}`;
            await FileProcessing.saveFileData(
                rootPath, 
                config.Site.headerFileName, 
                updatedContent, 
                'text/html'
            );

            // Update local state
            setPageContents(prev => ({
                ...prev,
                [currentPage.id]: {
                    ...prev[currentPage.id],
                    headerHTML: updatedContent
                }
            }));

            setEditPageIndex(null);
        } catch (error) {
            console.error("Failed to save page content:", error);
        }
    }, [currentPage, FileProcessing, config.Site.siteName, config.Site.headerFileName]);

    // Image upload handler
    const handleImageUpload = useCallback(async (blobInfo, success, failure) => {
        try {
            const folder = `assets/${config.Site.siteName}/images`;
            const fileData = blobInfo.blob();
            await FileProcessing.saveFileData(folder, blobInfo.filename(), fileData, fileData.type);
            const fullFilename = `${config.Site.appURL}public/${folder}/${blobInfo.filename()}`;
            success(fullFilename);
            return fullFilename;
        } catch (error) {
            console.error("Image upload failed:", error);
            failure("Upload failed");
        }
    }, [FileProcessing, config.Site.siteName, config.Site.appURL]);

    // Reset edit state on page change
    useEffect(() => {
        setEditPageIndex(null);
    }, [currentPageIndex]);

    // Render page content
    const renderPageContent = useCallback(() => {
        if (!currentPage || !pageContents[currentPage.id]) {
            return (
                <Box display="flex" justifyContent="center" alignItems="center" height="400px">
                    <Typography variant="h6" color="textSecondary">
                        {loadingStates[currentPage?.id] ? 'Loading...' : 'No content available'}
                    </Typography>
                </Box>
            );
        }

        const { headerHTML, articles } = pageContents[currentPage.id];

        return (
            <div className="page-gallery-content" key={currentPage.id}>
                {/* Header Section (optional inside gallery) */}
                {showHeaderContent && (
                    <div className="page-gallery-header">
                        {editPageIndex === currentPageIndex ? (
                            <div className="page-gallery-editor">
                                <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
                                    <Typography variant="h6">Editing Page Content</Typography>
                                    <IconButton onClick={() => setEditPageIndex(null)}>
                                        <Close />
                                    </IconButton>
                                </Box>
                                <HtmlEditor
                                    initialHtml={headerHTML}
                                    onSave={handlePageSave}
                                    cssfilePath={dynamicCSSURL}
                                    imagesUploadHandler={handleImageUpload}
                                    images={imageList}
                                />
                            </div>
                        ) : (
                            <div className="page-gallery-header-content">
                                {headerHTML && <HtmlContentRenderer htmlContent={headerHTML} />}
                                {WebSiteState.isLoggedIn() && (
                                    <IconButton
                                        className="page-gallery-edit-button"
                                        onClick={() => handlePageEdit(currentPageIndex)}
                                        size="small"
                                    >
                                        <Edit />
                                    </IconButton>
                                )}
                            </div>
                        )}
                    </div>
                )}

                {/* Articles Section */}
                {articles && articles.length > 0 && (
                    <div className="page-gallery-articles">
                        {articles.map((article, index) => (
                            <Paper key={`${currentPage.id}-${article.id || index}`} className="page-gallery-article" elevation={2}>
                                <Box p={2}>
                                    {/* Article name intentionally hidden in gallery view */}
                                    {article.description && (
                                        <Typography variant="body2" color="textSecondary" gutterBottom>
                                            {article.description}
                                        </Typography>
                                    )}
                                    <ArticleEditor
                                        article={article}
                                        siteInfo={config}
                                        cssfilePath={dynamicCSSURL}
                                        images={imageList}
                                        onImageUploaded={handleImageUpload}
                                    />
                                </Box>
                            </Paper>
                        ))}
                    </div>
                )}
            </div>
        );
    }, [currentPage, pageContents, loadingStates, editPageIndex, currentPageIndex, dynamicCSSURL, imageList, WebSiteState, handlePageEdit, handlePageSave, handleImageUpload, config, showHeaderContent]);

    if (!pages || pages.length === 0) {
        return (
            <Box display="flex" justifyContent="center" alignItems="center" height="400px">
                <Typography variant="h6" color="textSecondary">
                    No pages available
                </Typography>
            </Box>
        );
    }

    return (
        <div
            className="page-gallery-container"
            onTouchStart={handleTouchStart}
            onTouchEnd={handleTouchEnd}
            onMouseDown={handleMouseDown}
            onMouseMove={handleMouseMove}
            onMouseUp={endMouseDrag}
            onMouseLeave={endMouseDrag}
        >
            {/* Navigation Header */}
            {showHeader && (
                <Box className="page-gallery-navigation" display="flex" alignItems="center" justifyContent="space-between" p={2}>
                    <IconButton
                        onClick={handlePreviousPage}
                        disabled={currentPageIndex === 0}
                        className="page-gallery-nav-button"
                    >
                        <ChevronLeft />
                    </IconButton>

                    <Box display="flex" alignItems="center" gap={2}>
                        <Typography variant="h6">
                            {currentPage?.name || 'Untitled Page'}
                        </Typography>
                        <Chip 
                            label={`${currentPageIndex + 1} of ${pages.length}`}
                            size="small"
                            color="primary"
                            variant="outlined"
                        />
                    </Box>

                    <IconButton
                        onClick={handleNextPage}
                        disabled={currentPageIndex === pages.length - 1}
                        className="page-gallery-nav-button"
                    >
                        <ChevronRight />
                    </IconButton>
                </Box>
            )}

            {/* Page Content */}
            <div className="page-gallery-content-wrapper">
                {renderPageContent()}
            </div>

            {/* Page Indicators */}
            <Box className="page-gallery-indicators" display="flex" justifyContent="center" p={2}>
                {pages.map((_, index) => (
                    <Box
                        key={index}
                        className={`page-gallery-indicator ${index === currentPageIndex ? 'active' : ''}`}
                        onClick={() => setCurrentPageIndex(index)}
                        sx={{
                            width: 12,
                            height: 12,
                            borderRadius: '50%',
                            backgroundColor: index === currentPageIndex ? 'primary.main' : 'grey.300',
                            margin: '0 4px',
                            cursor: 'pointer',
                            transition: 'background-color 0.2s'
                        }}
                    />
                ))}
            </Box>
        </div>
    );
};

export default PageGallery; 
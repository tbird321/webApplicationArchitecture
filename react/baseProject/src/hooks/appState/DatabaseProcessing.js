import { API } from 'aws-amplify';
import { FileProcessing } from './FileProcessing';

const generateUUID = () => {
    if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
        return crypto.randomUUID();
    }
    return `id-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
};

export const DatabaseProcessing = {
    getWebsites: async () => {
        var pageInfo = await API.get('webArchApi', '/website/', {});
        return pageInfo;   
    },
    getPageById: async (pageId, websiteId) => {
        var pageInfo = await API.get('webArchApi', '/page/' + pageId + '/' + websiteId, {});
        return pageInfo;        
    },
    getArticleById: async (pageId) => {
        var pageInfo = await API.get('webArchApi', '/article/' + pageId, {});
        return pageInfo;
    },
    getPageByName: async (pageName,websiteId) => {
        var pageInfo = await API.get('webArchApi', '/page/search/' + pageName +'/'+websiteId, {});
        return pageInfo;
    },
    savePage: async (pageAndArticles) => {
        var pageInformation = { body: { ...pageAndArticles }};
        var pageInfo = await API.post('webArchApi', '/page', pageInformation);
        console.log(pageInfo);
        return pageInfo;
    },
    upsertArticle: async (article) => {
        var articleInfo = { body: { ...article } };
        var newArticleInfo = await API.post('webArchApi', '/article', articleInfo);
        console.log(newArticleInfo);
        return newArticleInfo;
    },
    searchPage: async (searchInfo) => {
        var tempSearchInfo = {
            body: {...searchInfo }
        };
        var pageInfo = [];
        try {
            pageInfo = await API.post('webArchApi', '/page/search', tempSearchInfo);
            console.log(pageInfo);
        }
        catch (e) {
            
        }
        console.log(pageInfo);
        return pageInfo;
    },
    searchCollection: async (searchInfo) => {
        var tempSearchInfo = {
            body: { ...searchInfo }
        };
        var collectionInfo = [];
        try {
            collectionInfo = await API.post('webArchApi', '/collection/search', tempSearchInfo);
            console.log(collectionInfo);
        }
        catch (e) {

        }
        console.log(collectionInfo);
        return collectionInfo;
    },
    searchArticle: async (searchInfo) => {
        var tempSearchInfo = {
            body: { ...searchInfo }
        };
        var pageInfo = await API.post('webArchApi', '/article/search', tempSearchInfo);
        console.log(pageInfo);
        return pageInfo;
    },
    getKeywords: async () => {
        var pageInfo = await API.get('webArchApi', '/keyword', {});
        return pageInfo;
    },
    getTopics: async () => {
        var pageInfo = await API.get('webArchApi', '/topic', {});
        return pageInfo;
    },
    getLayouts: async () => {
        var pageInfo = await API.get('webArchApi', '/layout', {});
        return pageInfo;
    },
    upsertCollections: async (collection) => {
        var collectionInfo = { body: { ...collection } };
        var newcollectionInfo = await API.post('webArchApi', '/collection', collectionInfo);
        return newcollectionInfo;
    },
    getCollectionPage: async (collectionId) => {
        var newcollectionInfo = await API.get('webArchApi', '/collection/' + collectionId, {});
        return newcollectionInfo;
    },
    DeleteCollection: async (collectionId) => {
        var newcollectionInfo = await API.del('webArchApi', '/collection/' + collectionId, {});
        return newcollectionInfo;
    },
    DeletePageFromCollection: async (collectionId, pageId) => {
        var newcollectionInfo = await API.del('webArchApi', '/collection/' + collectionId + '/page/' + pageId, {});
        return newcollectionInfo;
    },
    AssociatePagesWithCollection: async (collectionPageDetail) => {
        const path = '/collection/' + collectionPageDetail.collection.id + '/page';
        const body = { body: { ...collectionPageDetail } };
        try {
            const newcollectionInfo = await API.post('webArchApi', path, body);
            return newcollectionInfo;
        } catch (error) {
            const status = error?.response?.status || error?.$metadata?.httpStatusCode;
            console.error('AssociatePagesWithCollection failed', { path, status, body, error });
            throw error;
        }
    },
    publishPage: async (pageId) => {
        return await API.post('webArchApi', `/page/${pageId}/publish`, { body: {} });
    },
    unpublishPage: async (pageId) => {
        return await API.post('webArchApi', `/page/${pageId}/unpublish`, { body: {} });
    },
    publishArticle: async (articleId) => {
        return await API.post('webArchApi', `/article/${articleId}/publish`, { body: {} });
    },
    unpublishArticle: async (articleId) => {
        return await API.post('webArchApi', `/article/${articleId}/unpublish`, { body: {} });
    },
    createPageWithDefaultArticle: async (pageName, websiteId, config) => {
        const articleId = generateUUID();
        const articlePath = `${articleId}.html`;
        const articleName = `${pageName} - Content`;

        if (config?.Site?.siteName) {
            const rootPath = `websites/${config.Site.siteName}/articles`;
            await FileProcessing.saveFileData(
                rootPath,
                articlePath,
                `<div><h2>${pageName}</h2><p>Start writing here.</p></div>`,
                'text/html'
            );
        }

        const pageModel = {
            id: null,
            name: pageName,
            description: '',
            websiteId,
            keywords: [],
            topics: [],
            layout: '',
            layoutid: null,
            style: '',
            status: 'draft',
            articles: [{
                id: null,
                articleId,
                name: articleName,
                description: '',
                articlePath,
                memeImagePath: '',
                sequence_no: 1,
                keywords: [],
                topics: [],
                websiteId,
                status: 'draft'
            }]
        };

        return await API.post('webArchApi', '/page', { body: pageModel });
    },
};
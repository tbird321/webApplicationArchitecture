import React, { useState, useCallback } from 'react';
import { useAppStateContext } from '../../hooks/appState/useAppStateContext'; // Adjust the import path accordingly
import HtmlContentRenderer from './HtmlContentRenderer'; // Adjust the import path accordingly
import { HtmlEditor } from '@tbirdcomponents/reactcomponents'; // Adjust the import path accordingly
import EditIcon from '@mui/icons-material/Edit';
function ArticleEditor({ article, siteInfo, editMode, images, onImageUploaded, cssfilePath, styleClasses }) {
    const [curArticle, setCurArticle] = useState(article);
    const { FileProcessing } = useAppStateContext();
    const [articleEdit, setArticleEdit] = useState(false);
    const { WebSiteState } = useAppStateContext();
    // Use useCallback to memoize the function and prevent unnecessary re-renders
    const handleSaveArticleHtml = useCallback(async (htmlContent) => {
        const rootPath = `websites/${siteInfo.Site.siteName}`;
        await FileProcessing.saveFileData(`${rootPath}/articles`, curArticle.articlePath, htmlContent, 'text/html');

        // Update the state in an immutable way
        setCurArticle(prevArticle => ({ ...prevArticle, articleHTML: htmlContent }));
    }, [curArticle.articlePath, siteInfo, FileProcessing]); // Dependencies for useCallback


    const handleEditMode = () => {
        if (!articleEdit) {
            setArticleEdit(true);
        } else {
            setArticleEdit(false);
        }
    };

    // Render logic separated for clarity
    return editMode || articleEdit ? (
        <>
            <div style={{ float: 'right' }}>
                <EditIcon onClick={handleEditMode} />
            </div>
            <HtmlEditor
                cssfilePath={cssfilePath}
                styleClasses={styleClasses}
                initialHtml={curArticle.articleHTML}
                onSave={handleSaveArticleHtml}
                imagesUploadHandler={onImageUploaded}
                images={images}
            />
        </>
    ) : (
        <>
            {WebSiteState.isLoggedIn() &&
                <div style={{ float: 'right', cursor: 'pointer' }}>
                    <EditIcon onClick={handleEditMode} />
                </div>
            }
            <HtmlContentRenderer htmlContent={curArticle.articleHTML} />
        </>
    );
}

export default ArticleEditor;
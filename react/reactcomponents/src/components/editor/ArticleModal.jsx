import React, { useState, useEffect } from "react";
import Button from "@mui/material/Button";
import TextField from "@mui/material/TextField";
import Stack from "@mui/material/Stack";
import Autocomplete from "@mui/material/Autocomplete";
import ImageGallery from "./ImageGallery";
import ImageListItem from "@mui/material/ImageListItem";
import Tooltip from "@mui/material/Tooltip";
import "./ArticleModal.css";


const ArticleModal = ({ keywordOptions, topicsOptions, onSave, editArticle, imageList }) => {
    const [currentMeme, setCurrentMeme] = useState();
    const [showGallery, setShowGallery] = useState(false);


    const [newArticle, setNewArticle] = useState(editArticle ? { ...editArticle } : {
        name: "",
        description: "",
        keywords: [],
        topics: [],
        articleId: "",
        memeImagePath: "",
        articlePath: "",
    });
    useEffect(() => {
        setNewArticle(prev => ({
            ...prev,
            articlePath: `${prev.name.replace(/\s+/g, "-")}${prev.articleId ? `-${prev.articleId}` : ""}.html`
        }));
    }, [newArticle.name, newArticle.articleId]);

    const handleChange = (field, value) => {
        setNewArticle(prev => ({ ...prev, [field]: value }));
    };
    const handleSaveArticle = () => {
        onSave(newArticle);
    };
    const handleImageSelect = (image) => {
        handleChange("memeImagePath", image.filename);
        setCurrentMeme(image);
        setShowGallery(false);
    };
    const renderTextField = (field, label, type = "text") => (
        <TextField
            key={`${field}-key`}
            type={type}
            id={`${field}-standard-basic`}
            label={label}
            variant="standard"
            value={newArticle[field]}
            onChange={(e) => handleChange(field, e.target.value)}
        />
    );

    return (
        <div style={{ display: "flex", justifyContent: "space-between" }}>
            <div>
                <Stack direction='column' spacing={2} sx={{ width: 400 }}>
                    {renderTextField("name", "Name")}
                    {renderTextField("description", "Description")}
                    <Autocomplete
                        multiple
                        id="keywords-autocomplete"
                        options={keywordOptions??[]}
                        value={newArticle.keywords}
                        onChange={(event, newValue) => handleChange("keywords", newValue)}
                        renderInput={(params) => <TextField {...params} label="Keywords" variant="standard" />}
                    />
                    <Autocomplete
                        multiple
                        id="topics-autocomplete"
                        options={topicsOptions??[]}
                        value={newArticle.topics}
                        onChange={(event, newValue) => handleChange("topics", newValue)}
                        renderInput={(params) => <TextField {...params} label="Topics" variant="standard" />}
                    />
                    <Tooltip title="Selected image">
                        {currentMeme ? (
                            <ImageListItem className="imageItem" style={{ width: "100px", height: "100px" }}>
                                <img src={currentMeme.url} alt={currentMeme.alt || "Selected image"} loading="lazy" />
                            </ImageListItem>
                        ) : (
                            <div>No image selected</div>
                        )}
                    </Tooltip>
                    <Button onClick={() => setShowGallery(!showGallery)}>Select Image</Button>
                </Stack>
                <Button onClick={handleSaveArticle}>Save Article</Button>
            </div>
            {showGallery && (
                <ImageGallery
                    images={imageList}
                    onSelectImage={handleImageSelect}
                    width={"400px"}
                    height={"400px"}
                    columns={3}
                    rowHeight={100}
                    rowWidth={100}
                    gap={5}
                />
            )}
        </div>
    );
};

export default ArticleModal;

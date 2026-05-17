import React, { useState, useMemo } from "react";
import ImageList from "@mui/material/ImageList";
import TextField from "@mui/material/TextField";
import ImageListItem from "@mui/material/ImageListItem";
import Tooltip from "@mui/material/Tooltip";
import Pagination from "@mui/material/Pagination";
import "./ImageGallery.css";


const ImageGallery = ({ images, onSelectImage, width, height, columns, gap, rowWidth, rowHeight }) => {
    const [searchTerm, setSearchTerm] = useState("");
    const [currentPage, setCurrentPage] = useState(1);
    const itemsPerPage = 10; // Set the number of items per page
    const processImages = (images) =>
        images?.map(image => ({
            ...image,
            filename: image.url.split("/").pop().toLowerCase()
        }));

    const myImages = useMemo(() => processImages(images), [images]);

    const filteredImages = useMemo(() =>
        myImages.filter(image =>
            image.url && image.filename.includes(searchTerm.toLowerCase())
        ), [myImages, searchTerm]);

    const indexOfLastImage = currentPage * itemsPerPage;
    const indexOfFirstImage = indexOfLastImage - itemsPerPage;
    const currentImages = filteredImages.slice(indexOfFirstImage, indexOfLastImage); //Set the current page of rendered images

    const paginate = (pageNumber) => setCurrentPage(pageNumber);

    return (
        <div>
            <TextField
                type="text"
                placeholder="Search by filename..."
                variant="standard"
                value={searchTerm}
                onChange={e => setSearchTerm(e.target.value)}
            />
            <div className="scrollableImageList" style={{ height, width }}>
                <ImageList cols={columns} rowHeight={rowHeight} gap={gap}>
                    {currentImages?.map((image, index) => (
                        <Tooltip key={index} title={image.filename} placement="top">
                            <ImageListItem className="imageItem" onClick={() => onSelectImage(image)}>
                                <img src={image.url} alt={image.alt} loading="lazy" />
                            </ImageListItem>
                        </Tooltip>
                    ))}
                </ImageList>
            </div>
            <Pagination
                count={Math.ceil(filteredImages?.length??0 / itemsPerPage)}
                page={currentPage}
                onChange={(e, page) => paginate(page)}
                color="primary"
            />
        </div>
    );
};

export default ImageGallery;

//S3 File Processing Methods - amplify bucket, and s3 bucket processing
import { Storage } from 'aws-amplify';
export const FileProcessing = {   
    getFileURL: async (folder, fileName) => {
        const fullFileName = folder ? `${folder}/${fileName}` : fileName;
        // Retrieve the object from storage
        const fileUrl = await Storage.get(fullFileName);
        return fileUrl;
    },
    getFileList: async (folder) => {
        return await Storage.list(folder);
    },
    fileExists: async (folder, fileName) => {
        const fullFileName = folder ? `${folder}/${fileName}` : fileName;
        try {
            await Storage.get(fullFileName, { download: true });
            return true;
        } catch (error) {
            return false;
        }
    },
    getThemeNames: async (siteName) => {
        const folderPath = `assets/${siteName}/themes/`; // Specify the folder path
            try {
                const result = await Storage.list(folderPath);
                if (result) {
                    console.log('Themes retrieved successfully:', result);
                    const newThemesList = result.results
                        .map(item => {
                            const parts = item.key.split('/');
                            const filename = parts[parts.length - 1];
                            const tempFilename = filename.replace('.css', '').trim();
                            if (tempFilename === '') {
                                return null; // Return null for items with empty names
                            } else {
                                return {
                                    id: item.key,
                                    name: tempFilename // Extract the filename from the path
                                };
                            }
                        })
                        .filter(item => item !== null); // Filter out null items
                    return newThemesList;
                } else {
                    return [];
                }
            } catch (error) {
                console.error('Themes retrieval failed:', error);
                throw error;
            }
        },
    getFileData: async (folder, fileName) => {
        try {
            // Construct the full file path
            const fullFileName = folder ? `${folder}/${fileName}` : fileName;

            // Retrieve the object from storage
            const response = await Storage.get(fullFileName, { download: true });

            // Check if the response contains the expected 'Body' data
            if (response && response.Body) {
                // The 'Body' is a Buffer or a ReadableStream (Node.js environments)
                // For text content (like HTML), you can convert it to a string

                // If the environment is a browser and the Body is a Blob
                if (response.Body instanceof Blob) {
                    const fileContent = await new Response(response.Body).text();
                    return fileContent;
                }

                // If the Body is a Buffer or you know it's text content
                const fileContent = response.Body.toString(); // Convert Buffer to string
                return fileContent; // this should be your HTML content as a string
            } else {
                throw new Error('No file content available');
            }
        } catch (error) {
            console.error('An error occurred:', error);
            throw error; // Rethrow or handle error as appropriate for your application
        }
    },
    getFileDataObject: async (folder, fileName) => {
        const fullFileName = folder ? `${folder}/${fileName}` : fileName;
        try {
            const fileUrl = await Storage.get(fullFileName);
            const response = await fetch(fileUrl);
            const fileContent = await response.json();
            return fileContent;
        } catch (error) {
            console.error('Error getting file:', error);
        }
    },
    saveFileData: async (folder, fileName, fileData, contentType) => {
        const fullFileName = folder ? `${folder}/${fileName}` : fileName;
        try {
            return await Storage.put(fullFileName, fileData, {
                contentType: contentType == null ? 'application/json' : contentType
            });
        } catch (error) {
            console.error('Error saving file:', error);
            alert(error);
        }
        return false;
    },
    deleteFileData: async (folder, fileName) => {
        const fullFileName = folder ? `${folder}/${fileName}` : fileName;
        try {
            return await Storage.remove(fullFileName);
        } catch (error) {
            console.error('Error deleting file:', error);
            alert(error);
        }
        return false;
    }
    
    /*
AWS.config.update({
    accessKeyId: 'YOUR_ACCESS_KEY',
    secretAccessKey: 'YOUR_SECRET_KEY',
    region: 'us-west-2'
});

// Create an S3 instance
const s3 = new AWS.S3();

// Use this instance to interact with different buckets
s3.listObjects(
    { Bucket: 'my-first-bucket' },
    (err, data) => {  }
);

s3.listObjects(
    { Bucket: 'my-second-bucket' },
    (err, data) => {  }
);
    */
};   
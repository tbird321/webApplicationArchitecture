const path = require('path');

module.exports = {
    // Entry point for your application
    entry: './src/index.js',

    // Where to output the resulting bundle
    output: {
        filename: 'bundle.js',
        path: path.resolve(__dirname, 'dist'), // 'dist' is the common choice
        publicPath: '/', // This is important for the dev server to serve the index.html file
    },
    devServer: {
        contentBase: './dist', // where content is served from
        hot: true, // enable HMR (Hot Module Replacement)
        open: true, // opens browser automatically
        historyApiFallback: true, // for SPAs and using browser router
        // and any other settings you want to configure
    },

    // Set up loaders
    module: {
        rules: [
            {
                test: /\.(js|jsx)$/,
                exclude: /node_modules/,
                use: {
                    loader: 'babel-loader',
                    options: {
                        presets: ['@babel/preset-env', '@babel/preset-react'],
                    },
                },
            },
        ],
    },

    // Specify file extensions to resolve
    resolve: {
        extensions: ['.js', '.jsx'],
    },

    // Any additional plugins, if necessary
    // plugins: []
};

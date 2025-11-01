const path = require("path");
const CopyWebpackPlugin = require("copy-webpack-plugin");
const webpack = require("webpack");

module.exports = (env, argv) => {
  const isDevelopment = argv.mode === "development";

  return {
    entry: "./src/index.ts",
    output: {
      filename: "main.js",
      path: path.resolve(__dirname, "dist"),
    },
    devServer: {
      static: path.resolve(__dirname, "dist"),
      hot: true,
      port: 8080,
    },
    plugins: [
      new CopyWebpackPlugin({
        patterns: [{ from: "src/index.html", to: "index.html" }],
      }),
    ],
    module: {
      rules: [
        {
          test: /\.(css)$/,
          use: ["style-loader", "css-loader"],
        },
        {
          test: /\.ts?$/,
          use: "ts-loader",
          exclude: /node_modules/,
        },
      ],
    },
    resolve: {
      extensions: [".ts", ".js"],
    },
  };
};

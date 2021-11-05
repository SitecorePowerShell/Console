# How to generate resource files

* Restore Sitecore CLI

    ```bash
    dotnet tool restore
    ```

* Restore plugins for Sitecore CLI

    ```bash
    dotnet sitecore plugin list
    ```

* Generated resource files

    ```bash
    dotnet sitecore itemres create -o _out/spe --overwrite -i Spe.*
    ```
